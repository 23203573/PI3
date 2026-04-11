using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PensionatoApp.Data;
using PensionatoApp.Models;
using PensionatoApp.Services;
using PensionatoApp.ViewModels;

namespace PensionatoApp.Controllers
{
    [Authorize]
    public class ReservasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ContratoService _contratoService;

        public ReservasController(ApplicationDbContext context, ContratoService contratoService)
        {
            _context = context;
            _contratoService = contratoService;
        }

        // GET: Reservas
        public async Task<IActionResult> Index()
        {
            var reservas = await _context.Reservas
                .Include(r => r.Suite)
                .Include(r => r.Hospede)
                .Include(r => r.ReservaHospedes)
                    .ThenInclude(rh => rh.Hospede)
                .Include(r => r.Pagamentos)
                .OrderByDescending(r => r.DataReserva)
                .ToListAsync();
            
            return View(reservas);
        }

        // GET: Reservas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reserva = await _context.Reservas
                .Include(r => r.Suite)
                .Include(r => r.Hospede)
                .Include(r => r.Pagamentos)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reserva == null)
            {
                return NotFound();
            }

            return View(reserva);
        }

        // GET: Reservas/Create
        public async Task<IActionResult> Create()
        {
            var suitesDisponiveis = await _context.Suites
                .Where(s => s.Status == StatusSuite.Livre)
                .Select(s => new {
                    s.Id,
                    Display = $"Suíte {s.Numero:D2}: {s.PrecoMensal:C} - Mobiliado com box {(s.TipoCama == TipoBed.Casal ? "casal" : "solteiro")}"
                })
                .ToListAsync();
                
            ViewData["SuiteId"] = new SelectList(suitesDisponiveis, "Id", "Display");
            ViewData["HospedeId"] = new SelectList(
                await _context.Hospedes.Where(h => h.Ativo).ToListAsync(),
                "Id", "NomeCompleto");
            
            var reserva = new Reserva
            {
                DataEntrada = DateTime.Now,
                DataSaida = DateTime.Now.AddMonths(10).AddDays(DateTime.Now.Day == 29 || DateTime.Now.Day == 30 || DateTime.Now.Day == 31 ? -DateTime.Now.Day + 11 : 11 - DateTime.Now.Day) // Até janeiro do ano seguinte
            };

            return View(reserva);
        }

        // POST: Reservas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CriarReservaViewModel viewModel)
        {
            // Definir valores padrão se necessário
            if (viewModel.PrecoGaragem == 0)
                viewModel.PrecoGaragem = 120.00m;
            
            if (viewModel.PrecoArCondicionado == 0)
                viewModel.PrecoArCondicionado = 150.00m;

            // Validar se é suíte de casal e se tem dois hóspedes diferentes
            var suite = await _context.Suites.FindAsync(viewModel.SuiteId);
            if (suite != null && suite.TipoCama == TipoBed.Solteiro && viewModel.HospedeSecundarioId.HasValue)
            {
                ModelState.AddModelError("HospedeSecundarioId", "Suítes de solteiro só permitem 1 hóspede.");
            }

            if (viewModel.HospedeSecundarioId.HasValue && viewModel.HospedePrincipalId == viewModel.HospedeSecundarioId)
            {
                ModelState.AddModelError("HospedeSecundarioId", "O hóspede secundário deve ser diferente do principal.");
            }
                
            if (ModelState.IsValid)
            {
                try
                {
                    // Criar a reserva
                    var reserva = new Reserva
                    {
                        SuiteId = viewModel.SuiteId,
                        HospedeId = viewModel.HospedePrincipalId, // Mantido para compatibilidade
                        DataEntrada = viewModel.DataEntrada,
                        DataSaida = viewModel.DataSaida,
                        Origem = viewModel.Origem,
                        TemGaragem = viewModel.TemGaragem,
                        PrecoGaragem = viewModel.PrecoGaragem,
                        TemArCondicionado = viewModel.TemArCondicionado,
                        PrecoArCondicionado = viewModel.PrecoArCondicionado,
                        ValorAdiantado = viewModel.ValorAdiantado,
                        ValorCaucao = viewModel.ValorCaucao,
                        Observacoes = viewModel.Observacoes
                    };

                    // Calcular valor mensal total
                    if (suite != null)
                    {
                        reserva.ValorMensalTotal = suite.PrecoMensal;
                        
                        if (reserva.TemGaragem)
                            reserva.ValorMensalTotal += reserva.PrecoGaragem;
                            
                        if (reserva.TemArCondicionado)
                            reserva.ValorMensalTotal += reserva.PrecoArCondicionado;

                        // Atualizar status da suíte
                        suite.Status = StatusSuite.Ocupada;
                    }

                    _context.Add(reserva);
                    await _context.SaveChangesAsync();

                    // Criar relacionamentos na tabela ReservaHospedes
                    var reservaHospedePrincipal = new ReservaHospede
                    {
                        ReservaId = reserva.Id,
                        HospedeId = viewModel.HospedePrincipalId,
                        HospedePrincipal = true
                    };
                    _context.ReservaHospedes.Add(reservaHospedePrincipal);

                    if (viewModel.HospedeSecundarioId.HasValue)
                    {
                        var reservaHospedeSecundario = new ReservaHospede
                        {
                            ReservaId = reserva.Id,
                            HospedeId = viewModel.HospedeSecundarioId.Value,
                            HospedePrincipal = false
                        };
                        _context.ReservaHospedes.Add(reservaHospedeSecundario);
                    }

                    await _context.SaveChangesAsync();

                    // Criar pagamentos mensais
                    await CriarPagamentosMensais(reserva);

                    TempData["Sucesso"] = "Reserva criada com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Erro ao salvar a reserva: " + ex.Message);
                }
            }

            // Se chegou aqui, houve erro - recarregar as listas
            var suitesDisponiveis = await _context.Suites
                .Where(s => s.Status == StatusSuite.Livre)
                .Select(s => new {
                    s.Id,
                    s.TipoCama,
                    Display = $"Suíte {s.Numero:D2}: {s.PrecoMensal.ToString("C", new System.Globalization.CultureInfo("pt-BR"))} - Mobiliado com box {(s.TipoCama == TipoBed.Casal ? "casal (até 2 hóspedes)" : "solteiro (1 hóspede)")}"
                })
                .ToListAsync();
                
            ViewData["SuiteId"] = new SelectList(suitesDisponiveis, "Id", "Display", viewModel.SuiteId);
            ViewData["HospedePrincipalId"] = new SelectList(
                await _context.Hospedes.Where(h => h.Ativo).ToListAsync(),
                "Id", "NomeCompleto", viewModel.HospedePrincipalId);
            ViewData["HospedeSecundarioId"] = new SelectList(
                await _context.Hospedes.Where(h => h.Ativo).ToListAsync(),
                "Id", "NomeCompleto", viewModel.HospedeSecundarioId);
            
            return View(viewModel);
        }        // GET: Reservas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva == null)
            {
                return NotFound();
            }

            ViewData["SuiteId"] = new SelectList(
                await _context.Suites
                    .Select(s => new {
                        s.Id,
                        Display = $"Suíte {s.Numero:D2}: {s.PrecoMensal.ToString("C", new System.Globalization.CultureInfo("pt-BR"))} - Mobiliado com box {(s.TipoCama == TipoBed.Casal ? "casal" : "solteiro")}"
                    })
                    .ToListAsync(), 
                "Id", "Display", reserva.SuiteId);
            ViewData["HospedeId"] = new SelectList(
                await _context.Hospedes.ToListAsync(),
                "Id", "NomeCompleto", reserva.HospedeId);
            
            return View(reserva);
        }

        // POST: Reservas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,SuiteId,HospedeId,DataEntrada,DataSaida,Origem,Status,TemGaragem,PrecoGaragem,TemArCondicionado,PrecoArCondicionado,ValorMensalTotal,ValorAdiantado,ValorCaucao,Observacoes,DataReserva")] Reserva reserva)
        {
            if (id != reserva.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(reserva);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReservaExists(reserva.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["SuiteId"] = new SelectList(await _context.Suites.ToListAsync(), "Id", "Numero", reserva.SuiteId);
            ViewData["HospedeId"] = new SelectList(await _context.Hospedes.ToListAsync(), "Id", "NomeCompleto", reserva.HospedeId);
            
            return View(reserva);
        }

        // POST: Reservas/Finalizar/5
        [HttpPost]
        public async Task<IActionResult> Finalizar(int id)
        {
            var reserva = await _context.Reservas
                .Include(r => r.Suite)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reserva != null)
            {
                reserva.Status = StatusReserva.Finalizada;
                if (reserva.Suite != null)
                {
                    reserva.Suite.Status = StatusSuite.EmLimpeza;
                }

                // Marcar todas as notificações de checkout desta reserva como lidas
                var notificacoesCheckout = await _context.Notificacoes
                    .Where(n => n.ReservaId == reserva.Id && n.Tipo == TipoNotificacao.CheckOut)
                    .ToListAsync();
                    
                foreach (var notificacao in notificacoesCheckout)
                {
                    notificacao.Lida = true;
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task CriarPagamentosMensais(Reserva reserva)
        {
            var dataVencimento = reserva.DataEntrada.AddMonths(1);
            
            while (dataVencimento <= reserva.DataSaida)
            {
                var pagamento = new Pagamento
                {
                    ReservaId = reserva.Id,
                    DataVencimento = dataVencimento,
                    Valor = reserva.ValorMensalTotal,
                    Descricao = $"Mensalidade - {dataVencimento:MM/yyyy}",
                    Status = StatusPagamento.Pendente
                };

                _context.Pagamentos.Add(pagamento);
                dataVencimento = dataVencimento.AddMonths(1);
            }

            await _context.SaveChangesAsync();
        }

        // GET: Reservas/GerarContrato/5
        public async Task<IActionResult> GerarContrato(int? id)
        {
            if (id == null) return NotFound();

            var reserva = await _context.Reservas
                .Include(r => r.Suite)
                .Include(r => r.Hospede)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reserva == null) return NotFound();

            var contratoHtml = _contratoService.GerarContratoHtml(reserva);
            return Content(contratoHtml, "text/html");
        }

        private bool ReservaExists(int id)
        {
            return _context.Reservas.Any(e => e.Id == id);
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PensionatoApp.Data;
using PensionatoApp.Models;
using PensionatoApp.Services;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PensionatoApp.Controllers
{
    [Authorize]
    public class PagamentosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ContratoService _contratoService;
        private readonly PagamentoService _pagamentoService;

        public PagamentosController(ApplicationDbContext context, ContratoService contratoService, PagamentoService pagamentoService)
        {
            _context = context;
            _contratoService = contratoService;
            _pagamentoService = pagamentoService;
        }

        // GET: Pagamentos
        public async Task<IActionResult> Index(StatusPagamento? status)
        {
            var pagamentos = _context.Pagamentos
                .Include(p => p.Reserva)
                    .ThenInclude(r => r.Suite)
                .Include(p => p.Reserva)
                    .ThenInclude(r => r.Hospede)
                .AsQueryable();

            if (status.HasValue)
            {
                pagamentos = pagamentos.Where(p => p.Status == status.Value);
            }

            var resultado = await pagamentos
                .OrderBy(p => p.DataVencimento)
                .ToListAsync();

            ViewBag.StatusFiltro = status;
            return View(resultado);
        }

        // GET: Pagamentos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pagamento = await _context.Pagamentos
                .Include(p => p.Reserva)
                    .ThenInclude(r => r.Suite)
                .Include(p => p.Reserva)
                    .ThenInclude(r => r.Hospede)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pagamento == null)
            {
                return NotFound();
            }

            return View(pagamento);
        }

        // POST: Pagamentos/RegistrarPagamento/5
        [HttpPost]
        public async Task<IActionResult> RegistrarPagamento(int id, decimal valorPago, FormaPagamento formaPagamento, string? observacoes)
        {
            var pagamento = await _context.Pagamentos.FindAsync(id);
            
            if (pagamento != null)
            {
                pagamento.DataPagamento = DateTime.Now;
                pagamento.ValorPago = valorPago;
                pagamento.FormaPagamento = formaPagamento;
                pagamento.Status = StatusPagamento.Pago;
                pagamento.Observacoes = observacoes;

                await _context.SaveChangesAsync();
                
                TempData["Sucesso"] = "Pagamento registrado com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Pagamentos/Relatorio
        public async Task<IActionResult> Relatorio(string? dataInicio, string? dataFim)
        {
            DateTime dataInicioDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DateTime dataFimDate = dataInicioDate.AddMonths(1).AddDays(-1);

            // Converter strings do formato dd/MM/yyyy para DateTime
            if (!string.IsNullOrEmpty(dataInicio))
            {
                if (DateTime.TryParseExact(dataInicio, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime parsedInicio))
                {
                    dataInicioDate = parsedInicio;
                }
            }

            if (!string.IsNullOrEmpty(dataFim))
            {
                if (DateTime.TryParseExact(dataFim, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime parsedFim))
                {
                    dataFimDate = parsedFim;
                }
            }

            var pagamentos = await _context.Pagamentos
                .Include(p => p.Reserva)
                    .ThenInclude(r => r.Suite)
                .Include(p => p.Reserva)
                    .ThenInclude(r => r.Hospede)
                .Where(p => p.DataPagamento.HasValue && 
                           p.DataPagamento.Value.Date >= dataInicioDate.Date &&
                           p.DataPagamento.Value.Date <= dataFimDate.Date)
                .ToListAsync();

            var relatorio = new RelatorioFinanceiroViewModel
            {
                DataInicio = dataInicioDate,
                DataFim = dataFimDate,
                Pagamentos = pagamentos,
                TotalRecebido = pagamentos.Sum(p => p.ValorPago ?? 0),
                PagamentosPorForma = pagamentos
                    .Where(p => p.FormaPagamento.HasValue)
                    .GroupBy(p => p.FormaPagamento)
                    .ToDictionary(g => g.Key!.Value, g => g.Sum(p => p.ValorPago ?? 0))
            };

            return View(relatorio);
        }

        // GET: Pagamentos/GerarRecibo/5
        public async Task<IActionResult> GerarRecibo(int? id)
        {
            if (id == null) return NotFound();

            var pagamento = await _context.Pagamentos
                .Include(p => p.Reserva)
                    .ThenInclude(r => r.Suite)
                .Include(p => p.Reserva)
                    .ThenInclude(r => r.Hospede)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pagamento == null) return NotFound();

            var reciboHtml = _contratoService.GerarReciboHtml(pagamento);
            return Content(reciboHtml, "text/html");
        }

        // GET: Pagamentos/Pendentes
        public async Task<IActionResult> Pendentes()
        {
            await _pagamentoService.VerificarPagamentosAtrasados();
            var pagamentosPendentes = await _pagamentoService.ObterPagamentosPendentes();
            return View(pagamentosPendentes);
        }

        // POST: Pagamentos/Regularizar
        [HttpPost]
        public async Task<IActionResult> Regularizar(int id, decimal valorPago, FormaPagamento formaPagamento)
        {
            await _pagamentoService.RegularizarPagamento(id, valorPago, formaPagamento);
            TempData["Sucesso"] = "Pagamento regularizado com sucesso!";
            return RedirectToAction(nameof(Pendentes));
        }

        // GET: Pagamentos/ReceitaMensal
        public async Task<IActionResult> ReceitaMensal(int? ano, int? mes)
        {
            var hoje = DateTime.Today;
            var anoSelecionado = ano ?? hoje.Year;
            var mesSelecionado = mes ?? hoje.Month;

            var receita = await _pagamentoService.CalcularReceitaMensal(anoSelecionado, mesSelecionado);
            
            var viewModel = new ReceitaMensalViewModel
            {
                ReceitaAtual = receita,
                AnoSelecionado = anoSelecionado,
                MesSelecionado = mesSelecionado,
                Anos = Enumerable.Range(hoje.Year - 5, 10).ToList(),
                Meses = Enumerable.Range(1, 12).Select(i => new MesModel { 
                    Numero = i, 
                    Nome = new DateTime(2000, i, 1).ToString("MMMM", new System.Globalization.CultureInfo("pt-BR")) 
                }).ToList()
            };

            return View(viewModel);
        }

        // GET: Pagamentos/CadastrarManual
        public async Task<IActionResult> CadastrarManual()
        {
            ViewBag.Reservas = await _context.Reservas
                .Include(r => r.Hospede)
                .Include(r => r.Suite)
                .Where(r => r.Status == StatusReserva.Ativa)
                .Select(r => new { 
                    Value = r.Id, 
                    Text = $"{r.Hospede.NomeCompleto} - Suíte {r.Suite.Numero}" 
                })
                .ToListAsync();
            
            return View(new PagamentoManualViewModel());
        }

        // POST: Pagamentos/CadastrarManual
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CadastrarManual(PagamentoManualViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var pagamento = new Pagamento
                {
                    ReservaId = viewModel.ReservaId,
                    Valor = viewModel.Valor,
                    DataVencimento = viewModel.DataVencimento,
                    Descricao = viewModel.Descricao,
                    Status = StatusPagamento.Pendente,
                    DataCriacao = DateTime.Now,
                    Observacoes = viewModel.Observacoes
                };

                _context.Pagamentos.Add(pagamento);
                await _context.SaveChangesAsync();

                TempData["Sucesso"] = "Pagamento pendente cadastrado com sucesso!";
                return RedirectToAction(nameof(Index));
            }

            // Recarregar as reservas se houver erro
            ViewBag.Reservas = await _context.Reservas
                .Include(r => r.Hospede)
                .Include(r => r.Suite)
                .Where(r => r.Status == StatusReserva.Ativa)
                .Select(r => new { 
                    Value = r.Id, 
                    Text = $"{r.Hospede.NomeCompleto} - Suíte {r.Suite.Numero}" 
                })
                .ToListAsync();

            return View(viewModel);
        }

        public class MesModel
        {
            public int Numero { get; set; }
            public string Nome { get; set; } = "";
        }

        public class PagamentoManualViewModel
        {
            [Required(ErrorMessage = "A reserva é obrigatória")]
            [Display(Name = "Reserva")]
            public int ReservaId { get; set; }

            [Required(ErrorMessage = "O valor é obrigatório")]
            [Display(Name = "Valor")]
            [Column(TypeName = "decimal(10,2)")]
            [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero")]
            public decimal Valor { get; set; }

            [Required(ErrorMessage = "A data de vencimento é obrigatória")]
            [Display(Name = "Data de Vencimento")]
            [DataType(DataType.Date)]
            public DateTime DataVencimento { get; set; }

            [Required(ErrorMessage = "A descrição é obrigatória")]
            [Display(Name = "Descrição")]
            [StringLength(200, ErrorMessage = "A descrição não pode ter mais de 200 caracteres")]
            public string Descricao { get; set; } = "";

            [Display(Name = "Observações")]
            [StringLength(500, ErrorMessage = "As observações não podem ter mais de 500 caracteres")]
            public string? Observacoes { get; set; }
        }

        public class ReceitaMensalViewModel
        {
            public ReceitaMensal ReceitaAtual { get; set; } = new();
            public int AnoSelecionado { get; set; }
            public int MesSelecionado { get; set; }
            public List<int> Anos { get; set; } = new();
            public List<MesModel> Meses { get; set; } = new();
        }

        public class RelatorioFinanceiroViewModel
        {
            public DateTime DataInicio { get; set; }
            public DateTime DataFim { get; set; }
            public List<Pagamento> Pagamentos { get; set; } = new();
            public decimal TotalRecebido { get; set; }
            public Dictionary<FormaPagamento, decimal> PagamentosPorForma { get; set; } = new();
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PensionatoApp.Data;
using PensionatoApp.Models;

namespace PensionatoApp.Controllers
{
    [Authorize]
    public class NotificacoesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificacoesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Notificacoes
        public async Task<IActionResult> Index()
        {
            // Gerar notificações automáticas antes de exibir
            await GerarNotificacoesAutomaticas();

            var notificacoes = await _context.Notificacoes
                .Include(n => n.Reserva)
                    .ThenInclude(r => r.Hospede)
                .Include(n => n.Reserva)
                    .ThenInclude(r => r.Suite)
                .Include(n => n.Pagamento)
                    .ThenInclude(p => p.Reserva)
                        .ThenInclude(r => r.Suite)
                .OrderByDescending(n => n.DataCriacao)
                .ThenBy(n => n.Lida)
                .ToListAsync();

            ViewBag.NaoLidas = notificacoes.Count(n => !n.Lida);
            ViewBag.Total = notificacoes.Count;

            return View(notificacoes);
        }

        // POST: Notificacoes/MarcarComoLida/5
        [HttpPost]
        public async Task<IActionResult> MarcarComoLida(int id)
        {
            var notificacao = await _context.Notificacoes.FindAsync(id);
            if (notificacao != null && !notificacao.Lida)
            {
                notificacao.Lida = true;
                notificacao.DataCriacao = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        // POST: Notificacoes/MarcarTodasComoLidas
        [HttpPost]
        public async Task<IActionResult> MarcarTodasComoLidas()
        {
            var notificacoesNaoLidas = await _context.Notificacoes
                .Where(n => !n.Lida)
                .ToListAsync();

            foreach (var notificacao in notificacoesNaoLidas)
            {
                notificacao.Lida = true;
                notificacao.DataCriacao = DateTime.Now;
            }

            if (notificacoesNaoLidas.Any())
            {
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Notificacoes/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var notificacao = await _context.Notificacoes.FindAsync(id);
            if (notificacao != null)
            {
                _context.Notificacoes.Remove(notificacao);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Notificacoes/GetNotificacoesNavbar
        public async Task<IActionResult> GetNotificacoesNavbar()
        {
            await GerarNotificacoesAutomaticas();
            
            var notificacoesRecentes = await _context.Notificacoes
                .Where(n => !n.Lida)
                .Include(n => n.Reserva)
                    .ThenInclude(r => r.Suite)
                .OrderByDescending(n => n.DataCriacao)
                .Take(5)
                .ToListAsync();

            return PartialView("_NotificacoesNavbar", notificacoesRecentes);
        }

        private async Task GerarNotificacoesAutomaticas()
        {
            var hoje = DateTime.Now.Date;
            var dataLimiteVencimento = hoje.AddDays(3); // Avisar 3 dias antes

            // 1. Verificar reservas que precisam de baixa (prazo vencido)
            var reservasVencidas = await _context.Reservas
                .Include(r => r.Hospede)
                .Include(r => r.Suite)
                .Where(r => r.Status == StatusReserva.Ativa && r.DataSaida < hoje)
                .ToListAsync();

            foreach (var reserva in reservasVencidas)
            {
                // Verificar se já existe notificação para hoje
                var notificacaoHoje = await _context.Notificacoes
                    .AnyAsync(n => n.ReservaId == reserva.Id && 
                                  n.Tipo == TipoNotificacao.CheckOut && 
                                  n.DataCriacao.Date == hoje);

                if (!notificacaoHoje)
                {
                    // Marcar notificações anteriores como lidas
                    var notificacoesAnteriores = await _context.Notificacoes
                        .Where(n => n.ReservaId == reserva.Id && 
                                   n.Tipo == TipoNotificacao.CheckOut && 
                                   !n.Lida)
                        .ToListAsync();
                    
                    foreach (var notif in notificacoesAnteriores)
                    {
                        notif.Lida = true;
                    }

                    var diasAtraso = (hoje - reserva.DataSaida).Days;
                    await _context.Notificacoes.AddAsync(new Notificacao
                    {
                        Titulo = $"Check-out Atrasado - Suíte {reserva.Suite.Numero}",
                        Mensagem = $"A reserva de {reserva.Hospede?.NomeCompleto} na suíte {reserva.Suite.Numero} venceu há {diasAtraso} dia(s) ({reserva.DataSaida:dd/MM/yyyy}). É necessário fazer a baixa da reserva.",
                        Tipo = TipoNotificacao.CheckOut,
                        ReservaId = reserva.Id
                    });

                    // Manter a suíte como ocupada até o checkout manual
                    if (reserva.Suite != null && reserva.Suite.Status == StatusSuite.Livre)
                    {
                        reserva.Suite.Status = StatusSuite.Ocupada;
                    }
                }
            }

            // 2. Verificar reservas próximas ao vencimento
            var reservasProximasVencimento = await _context.Reservas
                .Include(r => r.Hospede)
                .Include(r => r.Suite)
                .Where(r => r.Status == StatusReserva.Ativa && 
                           r.DataSaida >= hoje && 
                           r.DataSaida <= dataLimiteVencimento)
                .ToListAsync();

            foreach (var reserva in reservasProximasVencimento)
            {
                var notificacaoExiste = await _context.Notificacoes
                    .AnyAsync(n => n.ReservaId == reserva.Id && n.Tipo == TipoNotificacao.VencimentoAluguel && !n.Lida);

                if (!notificacaoExiste)
                {
                    var diasRestantes = (reserva.DataSaida - hoje).Days;
                    var mensagem = diasRestantes == 0 ? 
                        $"A reserva de {reserva.Hospede?.NomeCompleto} na suíte {reserva.Suite.Numero} vence HOJE ({reserva.DataSaida:dd/MM/yyyy})." :
                        $"A reserva de {reserva.Hospede?.NomeCompleto} na suíte {reserva.Suite.Numero} vence em {diasRestantes} dia(s) ({reserva.DataSaida:dd/MM/yyyy}).";

                    await _context.Notificacoes.AddAsync(new Notificacao
                    {
                        Titulo = $"Vencimento Próximo - Suíte {reserva.Suite.Numero}",
                        Mensagem = mensagem,
                        Tipo = TipoNotificacao.VencimentoAluguel,
                        ReservaId = reserva.Id
                    });
                }
            }

            // 3. Verificar suítes que precisam de manutenção
            var suitesManutencao = await _context.Suites
                .Where(s => s.Status == StatusSuite.EmManutencao)
                .ToListAsync();

            foreach (var suite in suitesManutencao)
            {
                var notificacaoExiste = await _context.Notificacoes
                    .AnyAsync(n => n.Tipo == TipoNotificacao.Manutencao && 
                                  n.Mensagem.Contains($"Suíte {suite.Numero}") && 
                                  !n.Lida);

                if (!notificacaoExiste)
                {
                    await _context.Notificacoes.AddAsync(new Notificacao
                    {
                        Titulo = $"Manutenção Pendente - Suíte {suite.Numero}",
                        Mensagem = $"A suíte {suite.Numero} está marcada para manutenção. Verifique o andamento dos serviços e atualize o status quando concluído.",
                        Tipo = TipoNotificacao.Manutencao
                    });
                }
            }

            // 4. Verificar suítes que precisam de limpeza
            var suitesLimpeza = await _context.Suites
                .Where(s => s.Status == StatusSuite.EmLimpeza)
                .ToListAsync();

            foreach (var suite in suitesLimpeza)
            {
                var notificacaoExiste = await _context.Notificacoes
                    .AnyAsync(n => n.Tipo == TipoNotificacao.Limpeza && 
                                  n.Mensagem.Contains($"Suíte {suite.Numero}") && 
                                  !n.Lida);

                if (!notificacaoExiste)
                {
                    await _context.Notificacoes.AddAsync(new Notificacao
                    {
                        Titulo = $"Limpeza Pendente - Suíte {suite.Numero}",
                        Mensagem = $"A suíte {suite.Numero} está marcada para limpeza. Verifique o andamento da limpeza e atualize o status quando concluído.",
                        Tipo = TipoNotificacao.Limpeza
                    });
                }
            }

            // 5. Verificar pagamentos pendentes
            var pagamentosPendentes = await _context.Pagamentos
                .Include(p => p.Reserva)
                    .ThenInclude(r => r.Suite)
                .Include(p => p.Reserva)
                    .ThenInclude(r => r.Hospede)
                .Where(p => p.Status == StatusPagamento.Pendente && p.DataVencimento < hoje)
                .ToListAsync();

            foreach (var pagamento in pagamentosPendentes)
            {
                var notificacaoExiste = await _context.Notificacoes
                    .AnyAsync(n => n.PagamentoId == pagamento.Id && n.Tipo == TipoNotificacao.PagamentoPendente && !n.Lida);

                if (!notificacaoExiste)
                {
                    var diasAtraso = (hoje - pagamento.DataVencimento).Days;
                    await _context.Notificacoes.AddAsync(new Notificacao
                    {
                        Titulo = $"Pagamento Em Atraso - {pagamento.Reserva?.Hospede?.NomeCompleto}",
                        Mensagem = $"O pagamento de {pagamento.Valor:C} da suíte {pagamento.Reserva?.Suite?.Numero} está em atraso há {diasAtraso} dia(s). Vencimento: {pagamento.DataVencimento:dd/MM/yyyy}.",
                        Tipo = TipoNotificacao.PagamentoPendente,
                        PagamentoId = pagamento.Id
                    });
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
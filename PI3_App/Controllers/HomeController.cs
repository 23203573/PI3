using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PensionatoApp.Data;
using PensionatoApp.Models;

namespace PensionatoApp.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Gerar notificações automáticas
            await GerarNotificacoesAutomaticas();
            
            var dashboard = new DashboardViewModel
            {
                TotalSuites = await _context.Suites.CountAsync(),
                SuitesOcupadas = await _context.Suites.CountAsync(s => s.Status == StatusSuite.Ocupada),
                SuitesLivres = await _context.Suites.CountAsync(s => s.Status == StatusSuite.Livre),
                TotalHospedes = await _context.Hospedes.CountAsync(h => h.Ativo),
                ReservasAtivas = await _context.Reservas.CountAsync(r => r.Status == StatusReserva.Ativa),
                PagamentosPendentes = await _context.Pagamentos.CountAsync(p => p.Status == StatusPagamento.Pendente),
                ReceitaMensal = await _context.Pagamentos
                    .Where(p => p.Status == StatusPagamento.Pago && 
                               p.DataPagamento.HasValue &&
                               p.DataPagamento.Value.Month == DateTime.Now.Month &&
                               p.DataPagamento.Value.Year == DateTime.Now.Year)
                    .SumAsync(p => p.ValorPago ?? 0),
                NotificacoesPendentes = await _context.Notificacoes.CountAsync(n => !n.Lida)
            };

            return View(dashboard);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        private async Task GerarNotificacoesAutomaticas()
        {
            var hoje = DateTime.Now.Date;

            // Verificar reservas que precisam de baixa (prazo vencido)
            var reservasVencidas = await _context.Reservas
                .Include(r => r.Hospede)
                .Include(r => r.Suite)
                .Where(r => r.Status == StatusReserva.Ativa && r.DataSaida < hoje)
                .ToListAsync();

            foreach (var reserva in reservasVencidas)
            {
                var notificacaoExiste = await _context.Notificacoes
                    .AnyAsync(n => n.ReservaId == reserva.Id && n.Tipo == TipoNotificacao.CheckOut && !n.Lida);

                if (!notificacaoExiste)
                {
                    var diasAtraso = (hoje - reserva.DataSaida).Days;
                    await _context.Notificacoes.AddAsync(new Notificacao
                    {
                        Titulo = $"Check-out Atrasado - Suíte {reserva.Suite?.Numero}",
                        Mensagem = $"A reserva de {reserva.Hospede?.NomeCompleto} na suíte {reserva.Suite?.Numero} venceu há {diasAtraso} dia(s) ({reserva.DataSaida:dd/MM/yyyy}). É necessário fazer a baixa da reserva.",
                        Tipo = TipoNotificacao.CheckOut,
                        ReservaId = reserva.Id
                    });
                }
            }

            // Verificar pagamentos pendentes
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
                        Titulo = $"Pagamento Em Atraso",
                        Mensagem = $"Pagamento de {pagamento.Valor:C} está em atraso há {diasAtraso} dia(s). Vencimento: {pagamento.DataVencimento:dd/MM/yyyy}.",
                        Tipo = TipoNotificacao.PagamentoPendente,
                        PagamentoId = pagamento.Id
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        public class DashboardViewModel
        {
            public int TotalSuites { get; set; }
            public int SuitesOcupadas { get; set; }
            public int SuitesLivres { get; set; }
            public int TotalHospedes { get; set; }
            public int ReservasAtivas { get; set; }
            public int PagamentosPendentes { get; set; }
            public decimal ReceitaMensal { get; set; }
            public int NotificacoesPendentes { get; set; }
        }
    }
}
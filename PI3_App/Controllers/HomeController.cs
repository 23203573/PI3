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
            
            var hoje = DateTime.Today;
            var totalSuites = await _context.Suites.CountAsync();
            var suitesOcupadasHoje = await _context.Reservas.CountAsync(r => 
                r.Status == StatusReserva.Ativa && 
                r.DataEntrada <= hoje && 
                r.DataSaida > hoje);
            
            var dashboard = new DashboardViewModel
            {
                TotalSuites = totalSuites,
                // Suítes ocupadas no dia atual (baseado em reservas ativas que abranjam hoje)
                SuitesOcupadas = suitesOcupadasHoje,
                // Suítes livres = total - ocupadas (independente do status da suíte)
                SuitesLivres = totalSuites - suitesOcupadasHoje,
                // Hóspedes ativos no dia atual (que têm reserva ativa hoje)
                TotalHospedes = await _context.Reservas
                    .Include(r => r.Hospede)
                    .CountAsync(r => 
                        r.Status == StatusReserva.Ativa && 
                        r.DataEntrada <= hoje && 
                        r.DataSaida > hoje &&
                        r.Hospede != null && r.Hospede.Ativo),
                // Reservas ativas no dia atual
                ReservasAtivas = await _context.Reservas.CountAsync(r => 
                    r.Status == StatusReserva.Ativa && 
                    r.DataEntrada <= hoje && 
                    r.DataSaida > hoje),
                PagamentosPendentes = await _context.Pagamentos.CountAsync(p => p.Status == StatusPagamento.Pendente),
                // Receita mensal: primeiro tenta pagamentos efetivados, senão usa valor das reservas
                ReceitaMensal = await _context.Pagamentos
                    .Include(p => p.Reserva)
                    .Where(p => p.Status == StatusPagamento.Pago && 
                               p.Reserva != null &&
                               p.Reserva.DataEntrada.Month == DateTime.Now.Month &&
                               p.Reserva.DataEntrada.Year == DateTime.Now.Year)
                    .SumAsync(p => p.ValorPago ?? 0),
                NotificacoesPendentes = await _context.Notificacoes.CountAsync(n => !n.Lida),
                DataSelecionada = DateTime.Today
            };

            // Se não há receita de pagamentos, calcular baseado no valor das reservas do mês
            if (dashboard.ReceitaMensal == 0)
            {
                dashboard.ReceitaMensal = await _context.Reservas
                    .Where(r => r.DataEntrada.Month == DateTime.Now.Month &&
                               r.DataEntrada.Year == DateTime.Now.Year &&
                               r.Status == StatusReserva.Ativa)
                    .SumAsync(r => r.ValorMensalTotal);
            }

            // Calcular ocupação para hoje
            var ocupacaoHoje = await CalcularOcupacaoPorData(DateTime.Today);
            dashboard.SuitesOcupadasData = ocupacaoHoje.SuitesOcupadas;
            dashboard.TaxaOcupacaoData = ocupacaoHoje.TaxaOcupacao;

            return View(dashboard);
        }

        [HttpGet]
        public async Task<IActionResult> ObterOcupacaoPorData(DateTime data)
        {
            var ocupacao = await CalcularOcupacaoPorData(data);
            return Json(new 
            {
                suitesOcupadas = ocupacao.SuitesOcupadas,
                totalSuites = ocupacao.TotalSuites,
                taxaOcupacao = ocupacao.TaxaOcupacao,
                data = data.ToString("dd/MM/yyyy")
            });
        }

        private async Task<OcupacaoInfo> CalcularOcupacaoPorData(DateTime data)
        {
            var totalSuites = await _context.Suites.CountAsync();
            
            // Contar suítes ocupadas na data específica
            var suitesOcupadas = await _context.Reservas
                .Where(r => r.DataEntrada <= data && 
                           r.DataSaida > data && 
                           r.Status == StatusReserva.Ativa)
                .CountAsync();

            var taxaOcupacao = totalSuites > 0 ? (suitesOcupadas * 100 / totalSuites) : 0;

            return new OcupacaoInfo
            {
                SuitesOcupadas = suitesOcupadas,
                TotalSuites = totalSuites,
                TaxaOcupacao = taxaOcupacao
            };
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
            public DateTime DataSelecionada { get; set; }
            public int SuitesOcupadasData { get; set; }
            public int TaxaOcupacaoData { get; set; }
        }

        public class OcupacaoInfo
        {
            public int SuitesOcupadas { get; set; }
            public int TotalSuites { get; set; }
            public int TaxaOcupacao { get; set; }
        }
    }
}
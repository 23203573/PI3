using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PensionatoApp.Data;
using PensionatoApp.Models;
using System.Globalization;

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
            var reservasAtivasHojeQuery = _context.Reservas
                .Where(r => r.Status == StatusReserva.Ativa &&
                            r.DataEntrada <= hoje &&
                            r.DataSaida > hoje);
            var pagamentosEmAbertoQuery = _context.Pagamentos
                .Where(p => (p.Status == StatusPagamento.Pendente || p.Status == StatusPagamento.Atrasado)
                         && p.DataVencimento.Month == hoje.Month
                         && p.DataVencimento.Year == hoje.Year);
            var receitasUltimosMeses = await ObterReceitaUltimosMesesAsync(6);
            var origensReserva = await ObterDistribuicaoOrigemAsync();
            var ocupacaoPorTipo = await ObterOcupacaoPorTipoSuiteAsync(hoje);
            var proximosCheckouts = await ObterProximosCheckoutsAsync(5, hoje);
            var topSuites = await ObterTopSuitesAsync(5);
            var ticketMedio = await reservasAtivasHojeQuery
                .Select(r => (decimal?)r.ValorMensalTotal)
                .AverageAsync() ?? 0;
            var valorEmAberto = await pagamentosEmAbertoQuery
                .SumAsync(p => (decimal?)p.Valor) ?? 0;
            var pagamentosEmAtraso = await _context.Pagamentos
                .CountAsync(p => (p.Status == StatusPagamento.Pendente || p.Status == StatusPagamento.Atrasado)
                              && p.Status != StatusPagamento.Futuro
                              && p.DataVencimento.Month == hoje.Month
                              && p.DataVencimento.Year == hoje.Year
                              && p.DataVencimento.Date < hoje.Date);
            var percentualOcupacaoGeral = totalSuites > 0
                ? Math.Round((decimal)suitesOcupadasHoje / totalSuites * 100, 1)
                : 0;
            
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
                ReservasAtivas = await reservasAtivasHojeQuery.CountAsync(),
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
                DataSelecionada = DateTime.Today,
                TaxaOcupacaoAtual = percentualOcupacaoGeral,
                TicketMedioAtivo = ticketMedio,
                ValorEmAberto = valorEmAberto,
                PagamentosEmAtraso = pagamentosEmAtraso,
                ReceitaProjetadaAtiva = await reservasAtivasHojeQuery.SumAsync(r => (decimal?)r.ValorMensalTotal) ?? 0,
                ReceitaUltimosMeses = receitasUltimosMeses,
                DistribuicaoOrigemReservas = origensReserva,
                OcupacaoPorTipoSuite = ocupacaoPorTipo,
                ProximosCheckouts = proximosCheckouts,
                TopSuitesMaisReservadas = topSuites
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

        private async Task<List<SerieMensalItem>> ObterReceitaUltimosMesesAsync(int quantidadeMeses)
        {
            var inicio = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-(quantidadeMeses - 1));

            var pagamentos = await _context.Pagamentos
                .Where(p => p.Status == StatusPagamento.Pago)
                .Where(p => (p.DataPagamento ?? p.DataCriacao) >= inicio)
                .GroupBy(p => new
                {
                    Ano = (p.DataPagamento ?? p.DataCriacao).Year,
                    Mes = (p.DataPagamento ?? p.DataCriacao).Month
                })
                .Select(g => new
                {
                    g.Key.Ano,
                    g.Key.Mes,
                    Valor = g.Sum(p => p.ValorPago ?? p.Valor)
                })
                .ToListAsync();

            var reservasFallback = pagamentos.Count == 0
                ? await _context.Reservas
                    .Where(r => r.DataEntrada >= inicio)
                    .GroupBy(r => new { r.DataEntrada.Year, r.DataEntrada.Month })
                    .Select(g => new SerieMensalLookupItem
                    {
                        Ano = g.Key.Year,
                        Mes = g.Key.Month,
                        Valor = g.Sum(r => r.ValorMensalTotal)
                    })
                    .ToListAsync()
                : new List<SerieMensalLookupItem>();

            var itens = new List<SerieMensalItem>();
            for (var i = 0; i < quantidadeMeses; i++)
            {
                var referencia = inicio.AddMonths(i);
                decimal valor = 0;

                var pagamentoMes = pagamentos.FirstOrDefault(p => p.Ano == referencia.Year && p.Mes == referencia.Month);
                if (pagamentoMes != null)
                {
                    valor = pagamentoMes.Valor;
                }
                else if (pagamentos.Count == 0)
                {
                    var reservaMes = reservasFallback.FirstOrDefault(r => r.Ano == referencia.Year && r.Mes == referencia.Month);
                    if (reservaMes != null)
                    {
                        valor = reservaMes.Valor;
                    }
                }

                itens.Add(new SerieMensalItem
                {
                    Rotulo = referencia.ToString("MMM/yy", new CultureInfo("pt-BR")),
                    Valor = valor
                });
            }

            return itens;
        }

        private async Task<List<DistribuicaoItem>> ObterDistribuicaoOrigemAsync()
        {
            var distribuicao = await _context.Reservas
                .GroupBy(r => r.Origem)
                .Select(g => new DistribuicaoItem
                {
                    Rotulo = g.Key.ToString(),
                    Valor = g.Count()
                })
                .ToListAsync();

            foreach (var origem in Enum.GetValues<OrigemReserva>())
            {
                if (!distribuicao.Any(d => d.Rotulo == origem.ToString()))
                {
                    distribuicao.Add(new DistribuicaoItem
                    {
                        Rotulo = origem.ToString(),
                        Valor = 0
                    });
                }
            }

            return distribuicao.OrderBy(d => d.Rotulo).ToList();
        }

        private async Task<List<TipoSuiteOcupacaoItem>> ObterOcupacaoPorTipoSuiteAsync(DateTime data)
        {
            var tipos = await _context.Suites
                .GroupBy(s => s.TipoCama)
                .Select(g => new
                {
                    Tipo = g.Key,
                    Total = g.Count(),
                    Ocupadas = g.Count(s => s.Reservas.Any(r => r.Status == StatusReserva.Ativa && r.DataEntrada <= data && r.DataSaida > data))
                })
                .ToListAsync();

            return tipos.Select(t => new TipoSuiteOcupacaoItem
            {
                Tipo = t.Tipo switch
                {
                    TipoBed.Casal => "Casal",
                    TipoBed.Beliche => "Beliche",
                    _ => "Solteiro"
                },
                Ocupadas = t.Ocupadas,
                Livres = t.Total - t.Ocupadas,
                Total = t.Total
            }).ToList();
        }

        private async Task<List<CheckOutItem>> ObterProximosCheckoutsAsync(int limite, DateTime data)
        {
            return await _context.Reservas
                .Include(r => r.Hospede)
                .Include(r => r.Suite)
                .Where(r => r.Status == StatusReserva.Ativa && r.DataSaida >= data)
                .OrderBy(r => r.DataSaida)
                .Take(limite)
                .Select(r => new CheckOutItem
                {
                    Hospede = r.Hospede != null ? r.Hospede.NomeCompleto : "Hóspede não informado",
                    Suite = r.Suite != null ? r.Suite.Numero.ToString() : "-",
                    DataSaida = r.DataSaida,
                    Origem = r.Origem.ToString()
                })
                .ToListAsync();
        }

        private async Task<List<TopSuiteItem>> ObterTopSuitesAsync(int limite)
        {
            return await _context.Reservas
                .Include(r => r.Suite)
                .GroupBy(r => new { r.SuiteId, Numero = r.Suite != null ? r.Suite.Numero : 0 })
                .Select(g => new TopSuiteItem
                {
                    Suite = g.Key.Numero == 0 ? "-" : $"Suíte {g.Key.Numero}",
                    QuantidadeReservas = g.Count(),
                    ReceitaAcumulada = g.Sum(r => r.ValorMensalTotal)
                })
                .OrderByDescending(s => s.QuantidadeReservas)
                .ThenByDescending(s => s.ReceitaAcumulada)
                .Take(limite)
                .ToListAsync();
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
                        Titulo = $"Check-out Atrasado - Suíte {reserva.Suite?.Numero}",
                        Mensagem = $"A reserva de {reserva.Hospede?.NomeCompleto} na suíte {reserva.Suite?.Numero} venceu há {diasAtraso} dia(s) ({reserva.DataSaida:dd/MM/yyyy}). É necessário fazer a baixa da reserva.",
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
            public decimal TaxaOcupacaoAtual { get; set; }
            public decimal TicketMedioAtivo { get; set; }
            public decimal ValorEmAberto { get; set; }
            public int PagamentosEmAtraso { get; set; }
            public decimal ReceitaProjetadaAtiva { get; set; }
            public List<SerieMensalItem> ReceitaUltimosMeses { get; set; } = new();
            public List<DistribuicaoItem> DistribuicaoOrigemReservas { get; set; } = new();
            public List<TipoSuiteOcupacaoItem> OcupacaoPorTipoSuite { get; set; } = new();
            public List<CheckOutItem> ProximosCheckouts { get; set; } = new();
            public List<TopSuiteItem> TopSuitesMaisReservadas { get; set; } = new();
        }

        public class SerieMensalItem
        {
            public string Rotulo { get; set; } = string.Empty;
            public decimal Valor { get; set; }
        }

        public class SerieMensalLookupItem
        {
            public int Ano { get; set; }
            public int Mes { get; set; }
            public decimal Valor { get; set; }
        }

        public class DistribuicaoItem
        {
            public string Rotulo { get; set; } = string.Empty;
            public int Valor { get; set; }
        }

        public class TipoSuiteOcupacaoItem
        {
            public string Tipo { get; set; } = string.Empty;
            public int Ocupadas { get; set; }
            public int Livres { get; set; }
            public int Total { get; set; }
        }

        public class CheckOutItem
        {
            public string Hospede { get; set; } = string.Empty;
            public string Suite { get; set; } = string.Empty;
            public DateTime DataSaida { get; set; }
            public string Origem { get; set; } = string.Empty;
        }

        public class TopSuiteItem
        {
            public string Suite { get; set; } = string.Empty;
            public int QuantidadeReservas { get; set; }
            public decimal ReceitaAcumulada { get; set; }
        }

        public class OcupacaoInfo
        {
            public int SuitesOcupadas { get; set; }
            public int TotalSuites { get; set; }
            public int TaxaOcupacao { get; set; }
        }
    }
}
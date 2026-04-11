using Microsoft.EntityFrameworkCore;
using PensionatoApp.Data;
using PensionatoApp.Models;

namespace PensionatoApp.Services
{
    public class PagamentoService
    {
        private readonly ApplicationDbContext _context;

        public PagamentoService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task VerificarPagamentosAtrasados()
        {
            var hoje = DateTime.Now.Date;
            var pagamentosAtrasados = await _context.Pagamentos
                .Include(p => p.Reserva)
                    .ThenInclude(r => r.Hospede)
                .Include(p => p.Reserva)
                    .ThenInclude(r => r.Suite)
                .Where(p => p.Status == StatusPagamento.Pendente && 
                           p.DataVencimento < hoje)
                .ToListAsync();

            foreach (var pagamento in pagamentosAtrasados)
            {
                var diasAtraso = (hoje - pagamento.DataVencimento).Days;
                
                if (diasAtraso > 0 && !pagamento.IsInadimplente)
                {
                    // Marcar como inadimplente
                    pagamento.IsInadimplente = true;
                    pagamento.DiasAtraso = diasAtraso;
                    pagamento.Status = StatusPagamento.Atrasado;
                }
                else if (pagamento.IsInadimplente)
                {
                    // Atualizar dias de atraso
                    pagamento.DiasAtraso = diasAtraso;
                }

                // Verificar se deve enviar nova notificação (uma vez por mês)
                if (pagamento.UltimaNotificacaoEnviada == null || 
                    pagamento.UltimaNotificacaoEnviada.Value.AddDays(30) <= hoje)
                {
                    await CriarNotificacaoInadimplencia(pagamento);
                    pagamento.UltimaNotificacaoEnviada = hoje;
                    pagamento.QuantidadeNotificacoes++;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<Pagamento>> ObterPagamentosPendentes()
        {
            return await _context.Pagamentos
                .Include(p => p.Reserva)
                    .ThenInclude(r => r.Hospede)
                .Include(p => p.Reserva)
                    .ThenInclude(r => r.Suite)
                .Where(p => p.Status == StatusPagamento.Pendente || 
                           p.Status == StatusPagamento.Atrasado)
                .OrderByDescending(p => p.DiasAtraso)
                .ThenBy(p => p.DataVencimento)
                .ToListAsync();
        }

        public async Task RegularizarPagamento(int pagamentoId, decimal valorPago, FormaPagamento formaPagamento)
        {
            var pagamento = await _context.Pagamentos.FindAsync(pagamentoId);
            if (pagamento == null) return;

            pagamento.Status = StatusPagamento.Pago;
            pagamento.DataPagamento = DateTime.Now;
            pagamento.ValorPago = valorPago;
            pagamento.FormaPagamento = formaPagamento;
            pagamento.IsInadimplente = false;
            pagamento.DiasAtraso = 0;

            await _context.SaveChangesAsync();
        }

        public async Task<ReceitaMensal> CalcularReceitaMensal(int ano, int mes)
        {
            var dataInicio = new DateTime(ano, mes, 1);
            var dataFim = dataInicio.AddMonths(1).AddDays(-1);

            var pagamentos = await _context.Pagamentos
                .Where(p => p.DataVencimento >= dataInicio && p.DataVencimento <= dataFim)
                .ToListAsync();

            var pagamentosRecebidos = pagamentos.Where(p => p.Status == StatusPagamento.Pago).ToList();
            var pagamentosPendentes = pagamentos.Where(p => p.Status != StatusPagamento.Pago).ToList();

            var receita = new ReceitaMensal
            {
                Ano = ano,
                Mes = mes,
                ReceitaTotal = pagamentos.Sum(p => p.Valor),
                ReceitaRecebida = pagamentosRecebidos.Sum(p => p.ValorPago ?? p.Valor),
                ReceitaPendente = pagamentosPendentes.Sum(p => p.Valor),
                QuantidadePagamentos = pagamentos.Count,
                QuantidadePagamentosRecebidos = pagamentosRecebidos.Count,
                QuantidadePagamentosPendentes = pagamentosPendentes.Count,
                DataCalculo = DateTime.Now
            };

            return receita;
        }

        public async Task CriarNotificacaoInadimplencia(Pagamento pagamento)
        {
            var titulo = $"Pagamento em Atraso - {pagamento.Reserva?.Hospede?.NomeCompleto}";
            var mensagem = $"Pagamento de {pagamento.Valor:C} vencido em {pagamento.DataVencimento:dd/MM/yyyy}. " +
                          $"Atraso: {pagamento.DiasAtraso} dias. Suíte: {pagamento.Reserva?.Suite?.Numero}";

            var notificacao = new Notificacao
            {
                Titulo = titulo,
                Mensagem = mensagem,
                Tipo = TipoNotificacao.PagamentoPendente,
                DataCriacao = DateTime.Now,
                Lida = false
            };

            _context.Notificacoes.Add(notificacao);
            await _context.SaveChangesAsync();
        }
    }
}
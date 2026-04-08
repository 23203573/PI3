using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PensionatoApp.Data;
using PensionatoApp.Models;
using PensionatoApp.Services;

namespace PensionatoApp.Controllers
{
    [Authorize]
    public class PagamentosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ContratoService _contratoService;

        public PagamentosController(ApplicationDbContext context, ContratoService contratoService)
        {
            _context = context;
            _contratoService = contratoService;
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

        // GET: Pagamentos/Pendentes
        public async Task<IActionResult> Pendentes()
        {
            var pagamentosPendentes = await _context.Pagamentos
                .Include(p => p.Reserva)
                    .ThenInclude(r => r.Suite)
                .Include(p => p.Reserva)
                    .ThenInclude(r => r.Hospede)
                .Where(p => p.Status == StatusPagamento.Pendente)
                .OrderBy(p => p.DataVencimento)
                .ToListAsync();

            return View(pagamentosPendentes);
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
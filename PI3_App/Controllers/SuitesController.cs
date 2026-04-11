using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PensionatoApp.Data;
using PensionatoApp.Models;

namespace PensionatoApp.Controllers
{
    [Authorize]
    public class SuitesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SuitesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Suites
        public async Task<IActionResult> Index()
        {
            var hoje = DateTime.Today;
            var todasSuites = await _context.Suites
                .Include(s => s.Reservas.Where(r => r.Status == StatusReserva.Ativa))
                    .ThenInclude(r => r.Hospede)
                .OrderBy(s => s.Numero)
                .ToListAsync();
            
            ViewBag.TotalSuites = todasSuites.Count();
            ViewBag.SuitesOcupadas = todasSuites.Count(s => s.Reservas.Any(r => 
                r.Status == StatusReserva.Ativa && 
                r.DataEntrada <= hoje && 
                r.DataSaida > hoje));
            ViewBag.SuitesLivres = todasSuites.Count(s => !s.Reservas.Any(r => 
                r.Status == StatusReserva.Ativa && 
                r.DataEntrada <= hoje && 
                r.DataSaida > hoje) && s.Status == StatusSuite.Livre);
            ViewBag.SuitesManutencao = todasSuites.Count(s => s.Status == StatusSuite.EmManutencao);
            ViewBag.SuitesLimpeza = todasSuites.Count(s => s.Status == StatusSuite.EmLimpeza);
            
            return View(todasSuites);
        }

        // GET: Suites/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var suite = await _context.Suites
                .Include(s => s.Reservas)
                    .ThenInclude(r => r.Hospede)
                .Include(s => s.RegistrosLimpeza)
                .Include(s => s.RegistrosManutencao)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (suite == null)
            {
                return NotFound();
            }

            return View(suite);
        }

        // GET: Suites/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Suites/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Numero,PrecoMensal,TipoCama,ArCondicionado,PrecoArCondicionado,Ventilador,WiFi,Geladeira,MesaEstudos,QuadrosDecorativos,ArmarioEmbutido,Status,Observacoes")] Suite suite)
        {
            if (ModelState.IsValid)
            {
                _context.Add(suite);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(suite);
        }

        // GET: Suites/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var suite = await _context.Suites.FindAsync(id);
            if (suite == null)
            {
                return NotFound();
            }
            return View(suite);
        }

        // POST: Suites/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Numero,PrecoMensal,TipoCama,ArCondicionado,PrecoArCondicionado,Ventilador,WiFi,Geladeira,MesaEstudos,QuadrosDecorativos,ArmarioEmbutido,Status,Observacoes")] Suite suite)
        {
            if (id != suite.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(suite);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SuiteExists(suite.Id))
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
            return View(suite);
        }

        // POST: Suites/UpdateStatus/5
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, StatusSuite status)
        {
            var suite = await _context.Suites.FindAsync(id);
            if (suite != null)
            {
                suite.Status = status;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Suites/ToggleLimpeza/5
        [HttpPost]
        public async Task<IActionResult> ToggleLimpeza(int id)
        {
            var suite = await _context.Suites.FindAsync(id);
            if (suite != null)
            {
                // Alterna entre EmLimpeza e Livre
                suite.Status = suite.Status == StatusSuite.EmLimpeza ? StatusSuite.Livre : StatusSuite.EmLimpeza;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Suites/ToggleManutencao/5
        [HttpPost]
        public async Task<IActionResult> ToggleManutencao(int id)
        {
            var suite = await _context.Suites.FindAsync(id);
            if (suite != null)
            {
                // Alterna entre EmManutencao e Livre
                suite.Status = suite.Status == StatusSuite.EmManutencao ? StatusSuite.Livre : StatusSuite.EmManutencao;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Suites/TodasSuites
        public async Task<IActionResult> TodasSuites()
        {
            var todasSuites = await _context.Suites
                .OrderBy(s => s.Numero)
                .ToListAsync();
            
            return View("TodasSuites", todasSuites);
        }

        // GET: Suites/Ocupacao
        public async Task<IActionResult> Ocupacao()
        {
            var suitesComReservas = await _context.Suites
                .Include(s => s.Reservas.Where(r => r.Status == StatusReserva.Ativa))
                    .ThenInclude(r => r.Hospede)
                .OrderBy(s => s.Numero)
                .ToListAsync();
            
            ViewBag.TotalSuites = suitesComReservas.Count();
            ViewBag.SuitesOcupadas = suitesComReservas.Count(s => s.Reservas.Any(r => r.Status == StatusReserva.Ativa));
            ViewBag.SuitesLivres = suitesComReservas.Count(s => !s.Reservas.Any(r => r.Status == StatusReserva.Ativa));
            
            return View(suitesComReservas);
        }

        // POST: Suites/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var suite = await _context.Suites
                .Include(s => s.Reservas)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (suite == null)
            {
                return NotFound();
            }

            // Verificar se há reservas ativas
            if (suite.Reservas.Any(r => r.Status == StatusReserva.Ativa))
            {
                TempData["ErrorMessage"] = "Não é possível excluir uma suíte que possui reservas ativas.";
                return RedirectToAction(nameof(TodasSuites));
            }

            try
            {
                _context.Suites.Remove(suite);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Suíte {suite.Numero} foi excluída com sucesso.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Erro ao excluir a suíte. Tente novamente.";
            }

            return RedirectToAction(nameof(TodasSuites));
        }

        // GET: Suites/SuitesLivres
        public async Task<IActionResult> SuitesLivres(string? dataInicio, string? dataFim)
        {
            DateTime dataInicioDate = DateTime.Now.Date;
            DateTime dataFimDate = DateTime.Now.Date.AddDays(30);

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

            var suitesLivres = await _context.Suites
                .Where(s => !s.Reservas.Any(r => r.Status == StatusReserva.Ativa &&
                                               ((r.DataEntrada <= dataFimDate && r.DataSaida >= dataInicioDate))))
                .OrderBy(s => s.Numero)
                .ToListAsync();

            var viewModel = new SuitesLivresViewModel
            {
                DataInicio = dataInicioDate,
                DataFim = dataFimDate,
                SuitesLivres = suitesLivres
            };

            return View(viewModel);
        }

        private bool SuiteExists(int id)
        {
            return _context.Suites.Any(e => e.Id == id);
        }

        public class SuitesLivresViewModel
        {
            public DateTime DataInicio { get; set; }
            public DateTime DataFim { get; set; }
            public List<Suite> SuitesLivres { get; set; } = new();
        }
    }
}
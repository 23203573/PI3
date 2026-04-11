using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PensionatoApp.Data;
using PensionatoApp.Models;

namespace PensionatoApp.Controllers
{
    [Authorize]
    public class HospedesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HospedesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Hospedes
        public async Task<IActionResult> Index()
        {
            var hospedes = await _context.Hospedes
                .Include(h => h.Reservas.Where(r => r.Status == StatusReserva.Ativa))
                    .ThenInclude(r => r.Suite)
                .OrderBy(h => h.NomeCompleto)
                .ToListAsync();
            
            return View(hospedes);
        }

        // GET: Hospedes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hospede = await _context.Hospedes
                .Include(h => h.Reservas)
                    .ThenInclude(r => r.Suite)
                .Include(h => h.Reservas)
                    .ThenInclude(r => r.Pagamentos)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (hospede == null)
            {
                return NotFound();
            }

            return View(hospede);
        }

        // GET: Hospedes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Hospedes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("NomeCompleto,EhBrasileiro,RG,CPF,TipoDocumentoEstrangeiro,NumeroDocumentoEstrangeiro,DataNascimento,Telefone,Email,Endereco,ContatoEmergenciaNome,ContatoEmergenciaTelefone,Observacoes")] Hospede hospede)
        {
            // Validação customizada baseada na nacionalidade
            if (hospede.EhBrasileiro)
            {
                if (string.IsNullOrWhiteSpace(hospede.RG))
                {
                    ModelState.AddModelError("RG", "RG é obrigatório para brasileiros.");
                }
                if (string.IsNullOrWhiteSpace(hospede.CPF))
                {
                    ModelState.AddModelError("CPF", "CPF é obrigatório para brasileiros.");
                }
                
                // Limpar campos de estrangeiro se for brasileiro
                hospede.TipoDocumentoEstrangeiro = null;
                hospede.NumeroDocumentoEstrangeiro = null;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(hospede.TipoDocumentoEstrangeiro))
                {
                    ModelState.AddModelError("TipoDocumentoEstrangeiro", "Tipo do documento é obrigatório para estrangeiros.");
                }
                if (string.IsNullOrWhiteSpace(hospede.NumeroDocumentoEstrangeiro))
                {
                    ModelState.AddModelError("NumeroDocumentoEstrangeiro", "Número do documento é obrigatório para estrangeiros.");
                }
                
                // Limpar campos brasileiros se for estrangeiro
                hospede.RG = null;
                hospede.CPF = null;
            }

            if (ModelState.IsValid)
            {
                _context.Add(hospede);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(hospede);
        }

        // GET: Hospedes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hospede = await _context.Hospedes.FindAsync(id);
            if (hospede == null)
            {
                return NotFound();
            }
            return View(hospede);
        }

        // POST: Hospedes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,NomeCompleto,EhBrasileiro,RG,CPF,TipoDocumentoEstrangeiro,NumeroDocumentoEstrangeiro,DataNascimento,Telefone,Email,Endereco,ContatoEmergenciaNome,ContatoEmergenciaTelefone,Observacoes,DataCadastro,Ativo")] Hospede hospede)
        {
            if (id != hospede.Id)
            {
                return NotFound();
            }

            // Validação customizada baseada na nacionalidade
            if (hospede.EhBrasileiro)
            {
                if (string.IsNullOrWhiteSpace(hospede.RG))
                {
                    ModelState.AddModelError("RG", "RG é obrigatório para brasileiros.");
                }
                if (string.IsNullOrWhiteSpace(hospede.CPF))
                {
                    ModelState.AddModelError("CPF", "CPF é obrigatório para brasileiros.");
                }
                
                // Limpar campos de estrangeiro se for brasileiro
                hospede.TipoDocumentoEstrangeiro = null;
                hospede.NumeroDocumentoEstrangeiro = null;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(hospede.TipoDocumentoEstrangeiro))
                {
                    ModelState.AddModelError("TipoDocumentoEstrangeiro", "Tipo do documento é obrigatório para estrangeiros.");
                }
                if (string.IsNullOrWhiteSpace(hospede.NumeroDocumentoEstrangeiro))
                {
                    ModelState.AddModelError("NumeroDocumentoEstrangeiro", "Número do documento é obrigatório para estrangeiros.");
                }
                
                // Limpar campos brasileiros se for estrangeiro
                hospede.RG = null;
                hospede.CPF = null;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(hospede);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HospedeExists(hospede.Id))
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
            return View(hospede);
        }

        // POST: Hospedes/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var hospede = await _context.Hospedes.FindAsync(id);
            if (hospede != null)
            {
                hospede.Ativo = !hospede.Ativo;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Hospedes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hospede = await _context.Hospedes.FindAsync(id);
            if (hospede != null)
            {
                hospede.Ativo = false; // Soft delete
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool HospedeExists(int id)
        {
            return _context.Hospedes.Any(e => e.Id == id);
        }
    }
}
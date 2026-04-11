using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PensionatoApp.Data;
using PensionatoApp.Models;

namespace PensionatoApp.Controllers
{
    public class PublicController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PublicController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: / (Página inicial pública)
        public async Task<IActionResult> Index(string? dataInicio, string? dataFim)
        {
            // Se usuário está logado, redirecionar para dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            DateTime dataInicioDate = DateTime.Now.Date;
            DateTime dataFimDate = DateTime.Now.Date.AddDays(30);

            // Converter strings para DateTime (aceita dd/MM/yyyy e yyyy-MM-dd)
            if (!string.IsNullOrEmpty(dataInicio))
            {
                if (DateTime.TryParseExact(dataInicio, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime parsedInicio))
                {
                    dataInicioDate = parsedInicio;
                }
                else if (DateTime.TryParseExact(dataInicio, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime parsedInicioISO))
                {
                    dataInicioDate = parsedInicioISO;
                }
            }

            if (!string.IsNullOrEmpty(dataFim))
            {
                if (DateTime.TryParseExact(dataFim, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime parsedFim))
                {
                    dataFimDate = parsedFim;
                }
                else if (DateTime.TryParseExact(dataFim, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime parsedFimISO))
                {
                    dataFimDate = parsedFimISO;
                }
            }

            var suitesDisponiveis = await _context.Suites
                .Where(s => !s.Reservas.Any(r => r.Status == StatusReserva.Ativa &&
                                               ((r.DataEntrada <= dataFimDate && r.DataSaida >= dataInicioDate))))
                .OrderBy(s => s.Numero)
                .ToListAsync();

            var viewModel = new SuitesDisponiveisViewModel
            {
                DataInicio = dataInicioDate,
                DataFim = dataFimDate,
                SuitesDisponiveis = suitesDisponiveis
            };

            return View(viewModel);
        }

        // GET: /Public/ReservarSuite/5
        public async Task<IActionResult> ReservarSuite(int? id, string? dataEntrada, string? dataSaida)
        {
            // Se usuário está logado, redirecionar para dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            if (id == null) return NotFound();

            var suite = await _context.Suites.FindAsync(id);
            if (suite == null) return NotFound();

            DateTime dataEntradaDate = DateTime.Now.Date;
            DateTime dataSaidaDate = DateTime.Now.Date.AddMonths(1);

            // Converter strings para DateTime (aceita dd/MM/yyyy e yyyy-MM-dd)
            if (!string.IsNullOrEmpty(dataEntrada))
            {
                if (DateTime.TryParseExact(dataEntrada, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime parsedEntrada))
                {
                    dataEntradaDate = parsedEntrada;
                }
                else if (DateTime.TryParseExact(dataEntrada, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime parsedEntradaISO))
                {
                    dataEntradaDate = parsedEntradaISO;
                }
            }

            if (!string.IsNullOrEmpty(dataSaida))
            {
                if (DateTime.TryParseExact(dataSaida, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime parsedSaida))
                {
                    dataSaidaDate = parsedSaida;
                }
                else if (DateTime.TryParseExact(dataSaida, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime parsedSaidaISO))
                {
                    dataSaidaDate = parsedSaidaISO;
                }
            }

            var reserva = new ReservaPublicaViewModel
            {
                SuiteId = suite.Id,
                Suite = suite,
                DataEntrada = dataEntradaDate,
                DataSaida = dataSaidaDate,
                // Dados do hóspede para preenchimento
                NomeCompleto = "",
                Email = "",
                Telefone = "",
                Documento = "",
                DataNascimento = DateTime.Now.AddYears(-20),
                Endereco = ""
            };

            return View(reserva);
        }

        // POST: /Public/ReservarSuite
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReservarSuite(ReservaPublicaFormViewModel form)
        {
            // Se usuário está logado, redirecionar para dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            // Converter as strings de data para DateTime
            DateTime dataEntrada = DateTime.Now.Date;
            DateTime dataSaida = DateTime.Now.Date.AddMonths(1);

            if (!string.IsNullOrEmpty(form.DataEntradaString))
            {
                if (!DateTime.TryParseExact(form.DataEntradaString, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out dataEntrada))
                {
                    ModelState.AddModelError("DataEntrada", "Data de entrada inválida. Use o formato dd/MM/yyyy.");
                }
            }

            if (!string.IsNullOrEmpty(form.DataSaidaString))
            {
                if (!DateTime.TryParseExact(form.DataSaidaString, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out dataSaida))
                {
                    ModelState.AddModelError("DataSaida", "Data de saída inválida. Use o formato dd/MM/yyyy.");
                }
            }

            // Criar o model com as datas convertidas
            var model = new ReservaPublicaViewModel
            {
                SuiteId = form.SuiteId,
                DataEntrada = dataEntrada,
                DataSaida = dataSaida,
                TemArCondicionado = form.TemArCondicionado,
                TemGaragem = form.TemGaragem,
                PrecoGaragem = form.PrecoGaragem,
                ValorCaucao = form.ValorCaucao,
                ValorAdiantado = form.ValorAdiantado,
                NomeCompleto = form.NomeCompleto,
                Email = form.Email,
                Telefone = form.Telefone,
                Documento = form.Documento, // Mantém para compatibilidade com a view
                DataNascimento = form.DataNascimento,
                Endereco = form.Endereco,
                ContatoEmergenciaNome = form.ContatoEmergenciaNome,
                ContatoEmergenciaTelefone = form.ContatoEmergenciaTelefone
            };

            // Validações de data no servidor
            if (model.DataEntrada < DateTime.Now.Date)
            {
                ModelState.AddModelError("DataEntrada", "A data de entrada não pode ser anterior ao dia de hoje.");
            }

            if (model.DataSaida < DateTime.Now.Date)
            {
                ModelState.AddModelError("DataSaida", "A data de saída não pode ser anterior ao dia de hoje.");
            }

            if (model.DataEntrada >= model.DataSaida)
            {
                ModelState.AddModelError("DataSaida", "A data de saída deve ser posterior à data de entrada.");
            }

            if (!ModelState.IsValid)
            {
                model.Suite = await _context.Suites.FindAsync(model.SuiteId);
                return View(model);
            }

            try
            {
                // Verificar se a suíte ainda está disponível no período
                var conflito = await _context.Reservas
                    .AnyAsync(r => r.SuiteId == model.SuiteId &&
                                  r.Status == StatusReserva.Ativa &&
                                  ((r.DataEntrada <= model.DataSaida && r.DataSaida >= model.DataEntrada)));

                if (conflito)
                {
                    ModelState.AddModelError("", "Esta suíte já está reservada para o período selecionado.");
                    model.Suite = await _context.Suites.FindAsync(model.SuiteId);
                    return View(model);
                }

                // Criar ou encontrar hóspede - ÁREA PÚBLICA: INATIVA EXISTENTE E CRIA NOVO
                var hospedeExistente = await BuscarHospedePorDocumentos(model.Documento);

                Hospede hospede;
                if (hospedeExistente != null)
                {
                    // Inativar o hóspede existente
                    hospedeExistente.Ativo = false;
                    _context.Update(hospedeExistente);
                    
                    // Criar novo hóspede ativo
                    hospede = new Hospede
                    {
                        NomeCompleto = model.NomeCompleto,
                        Email = model.Email,
                        Telefone = model.Telefone,
                        // Assumindo que reservas públicas são de brasileiros por padrão
                        EhBrasileiro = true,
                        CPF = model.Documento, // Por padrão, considera como CPF
                        DataNascimento = model.DataNascimento,
                        Endereco = model.Endereco,
                        ContatoEmergenciaNome = model.ContatoEmergenciaNome,
                        ContatoEmergenciaTelefone = model.ContatoEmergenciaTelefone,
                        DataCadastro = DateTime.Now,
                        Ativo = true
                    };
                }
                else
                {
                    // Criar novo hóspede se não existir
                    hospede = new Hospede
                    {
                        NomeCompleto = model.NomeCompleto,
                        Email = model.Email,
                        Telefone = model.Telefone,
                        // Assumindo que reservas públicas são de brasileiros por padrão
                        EhBrasileiro = true,
                        CPF = model.Documento, // Por padrão, considera como CPF
                        DataNascimento = model.DataNascimento,
                        Endereco = model.Endereco,
                        ContatoEmergenciaNome = model.ContatoEmergenciaNome,
                        ContatoEmergenciaTelefone = model.ContatoEmergenciaTelefone,
                        DataCadastro = DateTime.Now,
                        Ativo = true
                    };
                }
                
                _context.Hospedes.Add(hospede);
                await _context.SaveChangesAsync();

                // Buscar a suíte para calcular valores
                var suite = await _context.Suites.FindAsync(model.SuiteId);
                if (suite == null)
                {
                    ModelState.AddModelError("", "Suíte não encontrada.");
                    model.Suite = await _context.Suites.FindAsync(model.SuiteId);
                    return View(model);
                }
                
                // Calcular valor mensal total
                decimal valorMensal = suite.PrecoMensal;
                if (model.TemArCondicionado && suite.ArCondicionado)
                    valorMensal += suite.PrecoArCondicionado;
                if (model.TemGaragem)
                    valorMensal += model.PrecoGaragem;

                // Criar reserva
                var reserva = new Reserva
                {
                    SuiteId = model.SuiteId,
                    HospedeId = hospede.Id,
                    DataEntrada = model.DataEntrada,
                    DataSaida = model.DataSaida,
                    DataReserva = DateTime.Now,
                    Origem = OrigemReserva.Site,
                    Status = StatusReserva.Ativa,
                    TemArCondicionado = model.TemArCondicionado,
                    TemGaragem = model.TemGaragem,
                    PrecoGaragem = model.PrecoGaragem,
                    PrecoArCondicionado = model.TemArCondicionado ? suite.PrecoArCondicionado : 0,
                    ValorMensalTotal = valorMensal,
                    ValorCaucao = model.ValorCaucao,
                    ValorAdiantado = model.ValorAdiantado
                };

                _context.Reservas.Add(reserva);
                await _context.SaveChangesAsync();

                return RedirectToAction("ReservaConfirmada", new { id = reserva.Id });
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Erro ao processar a reserva. Tente novamente.");
                model.Suite = await _context.Suites.FindAsync(model.SuiteId);
                return View(model);
            }
        }

        // GET: /Public/ReservaConfirmada/5
        public async Task<IActionResult> ReservaConfirmada(int id)
        {
            var reserva = await _context.Reservas
                .Include(r => r.Suite)
                .Include(r => r.Hospede)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reserva == null) return NotFound();

            return View(reserva);
        }

        public class SuitesDisponiveisViewModel
        {
            public DateTime DataInicio { get; set; }
            public DateTime DataFim { get; set; }
            public List<Suite> SuitesDisponiveis { get; set; } = new();
            
            public string DataInicioFormatada => DataInicio.ToString("dd/MM/yyyy");
            public string DataFimFormatada => DataFim.ToString("dd/MM/yyyy");
        }

        public class ReservaPublicaViewModel
        {
            public int SuiteId { get; set; }
            public Suite? Suite { get; set; }
            
            // Dados da reserva
            public DateTime DataEntrada { get; set; }
            public DateTime DataSaida { get; set; }
            public bool TemArCondicionado { get; set; }
            public bool TemGaragem { get; set; }
            public decimal PrecoGaragem { get; set; } = 120.00m;
            public decimal ValorCaucao { get; set; } = 500.00m;
            public decimal ValorAdiantado { get; set; } = 0;
            
            // Propriedades para exibição formatada das datas
            public string DataEntradaFormatada => DataEntrada.ToString("dd/MM/yyyy");
            public string DataSaidaFormatada => DataSaida.ToString("dd/MM/yyyy");
            
            // Dados do hóspede
            public string NomeCompleto { get; set; } = "";
            public string Email { get; set; } = "";
            public string Telefone { get; set; } = "";
            public string Documento { get; set; } = "";
            public DateTime DataNascimento { get; set; }
            public string Endereco { get; set; } = "";
            public string ContatoEmergenciaNome { get; set; } = "";
            public string ContatoEmergenciaTelefone { get; set; } = "";
        }
        
        public class ReservaPublicaFormViewModel
        {
            public int SuiteId { get; set; }
            
            // Dados da reserva como strings
            public string DataEntradaString { get; set; } = "";
            public string DataSaidaString { get; set; } = "";
            public bool TemArCondicionado { get; set; }
            public bool TemGaragem { get; set; }
            public decimal PrecoGaragem { get; set; } = 120.00m;
            public decimal ValorCaucao { get; set; } = 500.00m;
            public decimal ValorAdiantado { get; set; } = 0;
            
            // Dados do hóspede
            public string NomeCompleto { get; set; } = "";
            public string Email { get; set; } = "";
            public string Telefone { get; set; } = "";
            public string Documento { get; set; } = "";
            public DateTime DataNascimento { get; set; }
            public string Endereco { get; set; } = "";
            public string ContatoEmergenciaNome { get; set; } = "";
            public string ContatoEmergenciaTelefone { get; set; } = "";
        }
        
        /// <summary>
        /// Busca hóspede por qualquer um dos documentos (CPF, RG ou documento estrangeiro)
        /// </summary>
        /// <param name="documento">Documento a buscar</param>
        /// <returns>Hóspede encontrado ou null</returns>
        private async Task<Hospede?> BuscarHospedePorDocumentos(string documento)
        {
            if (string.IsNullOrWhiteSpace(documento))
                return null;
                
            return await _context.Hospedes
                .FirstOrDefaultAsync(h => h.Ativo && 
                    (h.CPF == documento || 
                     h.RG == documento || 
                     h.NumeroDocumentoEstrangeiro == documento));
        }
    }
}
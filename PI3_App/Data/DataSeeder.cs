using Microsoft.EntityFrameworkCore;
using PensionatoApp.Models;

namespace PensionatoApp.Data
{
    public class DataSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly Random _random = new Random();

        public DataSeeder(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SeedDataAsync()
        {
            // Verificar se já existem dados
            if (await _context.Hospedes.AnyAsync())
            {
                Console.WriteLine("Banco já possui dados. Limpando dados existentes...");
                await LimparDadosExistentes();
            }

            Console.WriteLine("Iniciando população do banco de dados...");

            // Criar hóspedes
            var hospedes = await CriarHospedes();
            
            // Criar reservas
            await CriarReservas(hospedes);
            
            Console.WriteLine("População do banco concluída com sucesso!");
        }

        private async Task LimparDadosExistentes()
        {
            // Remover reservas e dados relacionados
            _context.ReservaHospedes.RemoveRange(_context.ReservaHospedes);
            _context.Pagamentos.RemoveRange(_context.Pagamentos);
            _context.Reservas.RemoveRange(_context.Reservas);
            _context.Hospedes.RemoveRange(_context.Hospedes);
            await _context.SaveChangesAsync();
        }

        private async Task<List<Hospede>> CriarHospedes()
        {
            var hospedes = new List<Hospede>();
            var nomes = new[]
            {
                "João Silva Santos", "Maria Oliveira Costa", "Carlos Eduardo Ferreira", "Ana Paula Rodrigues",
                "Pedro Henrique Lima", "Fernanda Alves Pereira", "Roberto Carlos Souza", "Juliana Mendes",
                "Lucas Gabriel Martins", "Camila Cristina Santos", "Rafael Augusto Lima", "Beatriz Fernandes",
                "Diego Almeida Silva", "Larissa Viana Costa", "Thiago Barbosa", "Gabriela Rocha",
                "Marcos Paulo Andrade", "Tatiana Moreira", "Gustavo Henrique Dias", "Priscila Santos",
                "James Anderson", "Sofia Gonzalez", "Pierre Dubois", "Yuki Tanaka", "Emma Wilson"
            };
            
            var emails = new[]
            {
                "joao.silva@email.com", "maria.costa@email.com", "carlos.ferreira@email.com", "ana.rodrigues@email.com",
                "pedro.lima@email.com", "fernanda.pereira@email.com", "roberto.souza@email.com", "juliana.mendes@email.com",
                "lucas.martins@email.com", "camila.santos@email.com", "rafael.lima@email.com", "beatriz.fernandes@email.com",
                "diego.silva@email.com", "larissa.costa@email.com", "thiago.barbosa@email.com", "gabriela.rocha@email.com",
                "marcos.andrade@email.com", "tatiana.moreira@email.com", "gustavo.dias@email.com", "priscila.santos@email.com",
                "james.anderson@email.com", "sofia.gonzalez@email.com", "pierre.dubois@email.com", "yuki.tanaka@email.com", "emma.wilson@email.com"
            };

            for (int i = 0; i < nomes.Length; i++)
            {
                bool ehBrasileiro = i < 20; // Primeiros 20 são brasileiros, últimos 5 são estrangeiros
                
                var hospede = new Hospede
                {
                    NomeCompleto = nomes[i],
                    Email = emails[i],
                    Telefone = GerarTelefone(),
                    EhBrasileiro = ehBrasileiro,
                    DataNascimento = GerarDataNascimento(),
                    Endereco = GerarEndereco(),
                    ContatoEmergenciaNome = GerarNomeContato(),
                    ContatoEmergenciaTelefone = GerarTelefone(),
                    DataCadastro = GerarDataCadastro(),
                    Ativo = true
                };

                if (ehBrasileiro)
                {
                    hospede.CPF = GerarCPF();
                    hospede.RG = GerarRG();
                }
                else
                {
                    hospede.TipoDocumentoEstrangeiro = GerarTipoDocumentoEstrangeiro();
                    hospede.NumeroDocumentoEstrangeiro = GerarNumeroDocumentoEstrangeiro();
                }

                hospedes.Add(hospede);
            }

            _context.Hospedes.AddRange(hospedes);
            await _context.SaveChangesAsync();
            
            Console.WriteLine($"Criados {hospedes.Count} hóspedes");
            return hospedes;
        }

        private async Task CriarReservas(List<Hospede> hospedes)
        {
            var suites = await _context.Suites.ToListAsync();
            var reservas = new List<Reserva>();
            var dataInicio = new DateTime(2026, 1, 1);
            var dataAtual = DateTime.Now.Date;

            // Criar reservas distribuídas ao longo do período
            var totalDias = (dataAtual - dataInicio).Days;
            var numReservas = _random.Next(15, 25); // Entre 15 e 25 reservas

            for (int i = 0; i < numReservas; i++)
            {
                var suite = suites[_random.Next(suites.Count)];
                var hospedePrincipal = hospedes[_random.Next(hospedes.Count)];
                
                // Gerar datas aleatórias
                var diasOffset = _random.Next(0, totalDias - 60);
                var dataEntrada = dataInicio.AddDays(diasOffset);
                var duracaoEstadia = _random.Next(7, 90); // Entre 1 semana e 3 meses
                var dataSaida = dataEntrada.AddDays(duracaoEstadia);
                
                // Verificar conflitos
                var temConflito = await _context.Reservas
                    .AnyAsync(r => r.SuiteId == suite.Id &&
                                  r.Status == StatusReserva.Ativa &&
                                  ((r.DataEntrada <= dataSaida && r.DataSaida >= dataEntrada)));

                if (temConflito) continue;

                var origem = _random.Next(0, 2) == 0 ? OrigemReserva.Presencial : OrigemReserva.Site;
                var temArCondicionado = suite.ArCondicionado && _random.Next(0, 3) == 0;
                var temGaragem = _random.Next(0, 4) == 0;
                
                decimal valorMensal = suite.PrecoMensal;
                if (temArCondicionado) valorMensal += suite.PrecoArCondicionado;
                if (temGaragem) valorMensal += 120m;

                var reserva = new Reserva
                {
                    SuiteId = suite.Id,
                    HospedeId = hospedePrincipal.Id,
                    DataEntrada = dataEntrada,
                    DataSaida = dataSaida,
                    DataReserva = dataEntrada.AddDays(-_random.Next(1, 30)),
                    Origem = origem,
                    Status = dataSaida < dataAtual ? StatusReserva.Finalizada : StatusReserva.Ativa,
                    TemArCondicionado = temArCondicionado,
                    TemGaragem = temGaragem,
                    PrecoGaragem = temGaragem ? 120m : 0,
                    PrecoArCondicionado = temArCondicionado ? suite.PrecoArCondicionado : 0,
                    ValorMensalTotal = valorMensal,
                    ValorCaucao = 500m,
                    ValorAdiantado = valorMensal / 2
                };

                reservas.Add(reserva);
            }

            _context.Reservas.AddRange(reservas);
            await _context.SaveChangesAsync();

            // Criar relacionamentos ReservaHospede (para suítes de casal com 2 pessoas)
            await CriarRelacionamentosReservaHospede(reservas, hospedes, suites);

            Console.WriteLine($"Criadas {reservas.Count} reservas");
        }

        private async Task CriarRelacionamentosReservaHospede(List<Reserva> reservas, List<Hospede> hospedes, List<Suite> suites)
        {
            var relacionamentos = new List<ReservaHospede>();

            foreach (var reserva in reservas)
            {
                var suite = suites.First(s => s.Id == reserva.SuiteId);
                
                // Adicionar hóspede principal
                relacionamentos.Add(new ReservaHospede
                {
                    ReservaId = reserva.Id,
                    HospedeId = reserva.HospedeId,
                    HospedePrincipal = true
                });

                // Para suítes de casal, 50% terão segundo hóspede
                if (suite.TipoCama == TipoBed.Casal && _random.Next(0, 2) == 0)
                {
                    var hospedeSecundario = hospedes
                        .Where(h => h.Id != reserva.HospedeId)
                        .OrderBy(x => _random.Next())
                        .First();

                    relacionamentos.Add(new ReservaHospede
                    {
                        ReservaId = reserva.Id,
                        HospedeId = hospedeSecundario.Id,
                        HospedePrincipal = false
                    });
                }
            }

            _context.ReservaHospedes.AddRange(relacionamentos);
            await _context.SaveChangesAsync();

            Console.WriteLine($"Criados {relacionamentos.Count} relacionamentos reserva-hóspede");
        }

        // Métodos auxiliares para gerar dados fictícios
        private string GerarTelefone()
        {
            return $"({_random.Next(11, 99)}) 9{_random.Next(1000, 9999)}-{_random.Next(1000, 9999)}";
        }

        private string GerarCPF()
        {
            return $"{_random.Next(100, 999)}.{_random.Next(100, 999)}.{_random.Next(100, 999)}-{_random.Next(10, 99)}";
        }

        private string GerarRG()
        {
            return $"{_random.Next(10, 99)}.{_random.Next(100, 999)}.{_random.Next(100, 999)}-{_random.Next(0, 9)}";
        }

        private DateTime GerarDataNascimento()
        {
            var anos = _random.Next(18, 65);
            return DateTime.Now.AddYears(-anos).AddDays(_random.Next(-180, 180));
        }

        private string GerarEndereco()
        {
            var ruas = new[] { "Rua das Flores", "Av. Paulista", "Rua Augusta", "Rua da Consolação", "Av. Rebouças", "Rua Oscar Freire" };
            var numero = _random.Next(10, 9999);
            var bairros = new[] { "Vila Madalena", "Pinheiros", "Jardins", "Centro", "Liberdade", "Mooca" };
            
            return $"{ruas[_random.Next(ruas.Length)]}, {numero} - {bairros[_random.Next(bairros.Length)]}, São Paulo/SP";
        }

        private string GerarNomeContato()
        {
            var nomes = new[] { "José Silva", "Maria Santos", "Carlos Oliveira", "Ana Costa", "Pedro Lima", "Fernanda Alves" };
            return nomes[_random.Next(nomes.Length)];
        }

        private DateTime GerarDataCadastro()
        {
            var diasAtras = _random.Next(30, 365);
            return DateTime.Now.AddDays(-diasAtras);
        }

        private string GerarTipoDocumentoEstrangeiro()
        {
            var tipos = new[] { "Passaporte", "RNE", "Carteira de Identidade", "Carteira de Motorista" };
            return tipos[_random.Next(tipos.Length)];
        }

        private string GerarNumeroDocumentoEstrangeiro()
        {
            return $"{_random.Next(100000000, 999999999)}";
        }
    }
}
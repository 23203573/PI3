using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PensionatoApp.Models
{
    public enum OrigemReserva
    {
        Site,
        Telefone,
        Presencial
    }

    public enum StatusReserva
    {
        Ativa,
        Finalizada,
        Cancelada
    }

    public class Reserva
    {
        public int Id { get; set; }
        
        [Required]
        public int SuiteId { get; set; }
        
        [Required]
        public int HospedeId { get; set; }
        
        [Required]
        public DateTime DataEntrada { get; set; }
        
        [Required]
        public DateTime DataSaida { get; set; }
        
        public OrigemReserva Origem { get; set; }
        
        public StatusReserva Status { get; set; } = StatusReserva.Ativa;
        
        public bool TemGaragem { get; set; } = false;
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal PrecoGaragem { get; set; } = 120.00m;
        
        public bool TemArCondicionado { get; set; } = false;
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal PrecoArCondicionado { get; set; } = 150.00m;
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal ValorMensalTotal { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal ValorAdiantado { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal ValorCaucao { get; set; }
        
        public string? Observacoes { get; set; }
        
        public DateTime DataReserva { get; set; } = DateTime.Now;
        
        // Relacionamentos - NÃO são obrigatórios para validação
        public virtual Suite? Suite { get; set; }
        public virtual Hospede? Hospede { get; set; }
        public virtual ICollection<ReservaHospede> ReservaHospedes { get; set; } = new List<ReservaHospede>();
        public virtual ICollection<Pagamento> Pagamentos { get; set; } = new List<Pagamento>();
    }
}
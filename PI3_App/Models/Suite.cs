using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PensionatoApp.Models
{
    public enum TipoBed
    {
        Solteiro,
        Casal
    }

    public enum StatusSuite
    {
        Livre,
        Ocupada,
        EmLimpeza,
        EmManutencao
    }

    public class Suite
    {
        public int Id { get; set; }
        
        [Required]
        public int Numero { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal PrecoMensal { get; set; }
        
        public TipoBed TipoCama { get; set; }
        
        public bool ArCondicionado { get; set; } = false;
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal PrecoArCondicionado { get; set; } = 150.00m;
        
        public bool Ventilador { get; set; } = true;
        
        public bool WiFi { get; set; } = true;
        
        public bool Geladeira { get; set; } = true;
        
        public bool MesaEstudos { get; set; } = true;
        
        public bool QuadrosDecorativos { get; set; } = true;
        
        public bool ArmarioEmbutido { get; set; } = true;
        
        public StatusSuite Status { get; set; } = StatusSuite.Livre;
        
        public string? Observacoes { get; set; }
        
        // Relacionamentos
        public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
        public virtual ICollection<RegistroLimpeza> RegistrosLimpeza { get; set; } = new List<RegistroLimpeza>();
        public virtual ICollection<RegistroManutencao> RegistrosManutencao { get; set; } = new List<RegistroManutencao>();
    }
}
using System.ComponentModel.DataAnnotations;

namespace PensionatoApp.Models
{
    public class ReservaHospede
    {
        public int Id { get; set; }
        
        [Required]
        public int ReservaId { get; set; }
        
        [Required]
        public int HospedeId { get; set; }
        
        public bool HospedePrincipal { get; set; } = false;
        
        // Relacionamentos
        public virtual Reserva Reserva { get; set; } = null!;
        public virtual Hospede Hospede { get; set; } = null!;
    }
}
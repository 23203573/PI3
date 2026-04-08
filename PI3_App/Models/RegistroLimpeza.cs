using System.ComponentModel.DataAnnotations;

namespace PensionatoApp.Models
{
    public class RegistroLimpeza
    {
        public int Id { get; set; }
        
        [Required]
        public int SuiteId { get; set; }
        
        [Required]
        public DateTime DataLimpeza { get; set; }
        
        [StringLength(100)]
        public string? ResponsavelLimpeza { get; set; }
        
        public string? Observacoes { get; set; }
        
        public DateTime DataRegistro { get; set; } = DateTime.Now;
        
        // Relacionamentos
        public virtual Suite Suite { get; set; } = null!;
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PensionatoApp.Models
{
    public class Hospede
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string NomeCompleto { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string Documento { get; set; } = string.Empty; // RG/CPF
        
        [Required]
        public DateTime DataNascimento { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Telefone { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? Endereco { get; set; }
        
        [StringLength(100)]
        public string? ContatoEmergenciaNome { get; set; }
        
        [StringLength(20)]
        public string? ContatoEmergenciaTelefone { get; set; }
        
        public string? Observacoes { get; set; }
        
        public DateTime DataCadastro { get; set; } = DateTime.Now;
        
        public bool Ativo { get; set; } = true;
        
        // Relacionamentos
        public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
}
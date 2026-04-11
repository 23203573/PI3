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
        
        // Documentação brasileira
        [StringLength(20)]
        public string? RG { get; set; }
        
        [StringLength(14)]
        public string? CPF { get; set; }
        
        // Campo para indicar se é brasileiro
        public bool EhBrasileiro { get; set; } = true;
        
        // Documentação estrangeira
        [StringLength(50)]
        public string? TipoDocumentoEstrangeiro { get; set; }
        
        [StringLength(50)]
        public string? NumeroDocumentoEstrangeiro { get; set; }
        
        [Required]
        public DateTime DataNascimento { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Telefone { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "O endereço é obrigatório")]
        [StringLength(200)]
        public string Endereco { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "O nome do contato de emergência é obrigatório")]
        [StringLength(100)]
        public string ContatoEmergenciaNome { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "O telefone do contato de emergência é obrigatório")]
        [StringLength(20)]
        public string ContatoEmergenciaTelefone { get; set; } = string.Empty;
        
        public string? Observacoes { get; set; }
        
        public DateTime DataCadastro { get; set; } = DateTime.Now;
        
        public bool Ativo { get; set; } = true;
        
        // Relacionamentos
        public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
        public virtual ICollection<ReservaHospede> ReservaHospedes { get; set; } = new List<ReservaHospede>();
    }
}
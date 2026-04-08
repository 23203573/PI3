using System.ComponentModel.DataAnnotations;

namespace PensionatoApp.Models
{
    public enum TipoManutencao
    {
        Eletrica,
        Hidraulica,
        Geral,
        ArCondicionado,
        Moveis
    }

    public enum StatusManutencao
    {
        Pendente,
        EmAndamento,
        Concluida,
        Cancelada
    }

    public class RegistroManutencao
    {
        public int Id { get; set; }
        
        [Required]
        public int SuiteId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Descricao { get; set; } = string.Empty;
        
        public TipoManutencao Tipo { get; set; }
        
        public StatusManutencao Status { get; set; } = StatusManutencao.Pendente;
        
        public DateTime DataSolicitacao { get; set; } = DateTime.Now;
        
        public DateTime? DataInicio { get; set; }
        
        public DateTime? DataConclusao { get; set; }
        
        [StringLength(100)]
        public string? ResponsavelManutencao { get; set; }
        
        public string? Observacoes { get; set; }
        
        // Relacionamentos
        public virtual Suite Suite { get; set; } = null!;
    }
}
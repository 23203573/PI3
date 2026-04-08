using System.ComponentModel.DataAnnotations;

namespace PensionatoApp.Models
{
    public enum TipoNotificacao
    {
        CheckIn,
        CheckOut,
        PagamentoPendente,
        ReservaFutura,
        VencimentoAluguel,
        Manutencao
    }

    public class Notificacao
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;
        
        [Required]
        public string Mensagem { get; set; } = string.Empty;
        
        public TipoNotificacao Tipo { get; set; }
        
        public DateTime DataCriacao { get; set; } = DateTime.Now;
        
        public bool Lida { get; set; } = false;
        
        public int? ReservaId { get; set; }
        
        public int? PagamentoId { get; set; }
        
        // Relacionamentos
        public virtual Reserva? Reserva { get; set; }
        public virtual Pagamento? Pagamento { get; set; }
    }
}
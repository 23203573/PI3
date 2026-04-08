using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PensionatoApp.Models
{
    public enum FormaPagamento
    {
        Dinheiro,
        Pix,
        Cheque,
        TransferenciaBancaria
    }

    public enum StatusPagamento
    {
        Pendente,
        Pago,
        Parcelado,
        Atrasado
    }

    public class Pagamento
    {
        public int Id { get; set; }
        
        [Required]
        public int ReservaId { get; set; }
        
        [Required]
        public DateTime DataVencimento { get; set; }
        
        public DateTime? DataPagamento { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Valor { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal? ValorPago { get; set; }
        
        public FormaPagamento? FormaPagamento { get; set; }
        
        public StatusPagamento Status { get; set; } = StatusPagamento.Pendente;
        
        [StringLength(100)]
        public string Descricao { get; set; } = string.Empty;
        
        public string? Observacoes { get; set; }
        
        public DateTime DataCriacao { get; set; } = DateTime.Now;
        
        // Relacionamentos
        public virtual Reserva Reserva { get; set; } = null!;
    }
}
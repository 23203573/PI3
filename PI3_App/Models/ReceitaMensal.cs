using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PensionatoApp.Models
{
    public class ReceitaMensal
    {
        public int Id { get; set; }
        
        [Required]
        public int Ano { get; set; }
        
        [Required]
        public int Mes { get; set; }
        
        [Column(TypeName = "decimal(12,2)")]
        public decimal ReceitaTotal { get; set; }
        
        [Column(TypeName = "decimal(12,2)")]
        public decimal ReceitaRecebida { get; set; }
        
        [Column(TypeName = "decimal(12,2)")]
        public decimal ReceitaPendente { get; set; }
        
        public int QuantidadePagamentos { get; set; }
        
        public int QuantidadePagamentosRecebidos { get; set; }
        
        public int QuantidadePagamentosPendentes { get; set; }
        
        public DateTime DataCalculo { get; set; } = DateTime.Now;
        
        [StringLength(100)]
        public string MesAno => $"{Mes:D2}/{Ano}";
        
        public string NomeMes => new DateTime(Ano, Mes, 1).ToString("MMMM", new System.Globalization.CultureInfo("pt-BR"));
    }
}
using PensionatoApp.Models;
using System.ComponentModel.DataAnnotations;

namespace PensionatoApp.ViewModels
{
    public class CriarReservaViewModel
    {
        [Required]
        public int SuiteId { get; set; }
        
        [Required]
        public int HospedePrincipalId { get; set; }
        
        public int? HospedeSecundarioId { get; set; }
        
        [Required]
        public DateTime DataEntrada { get; set; }
        
        [Required]
        public DateTime DataSaida { get; set; }
        
        public OrigemReserva Origem { get; set; }
        
        public bool TemGaragem { get; set; } = false;
        
        public decimal PrecoGaragem { get; set; } = 120.00m;
        
        public bool TemArCondicionado { get; set; } = false;
        
        public decimal PrecoArCondicionado { get; set; } = 150.00m;
        
        public decimal ValorAdiantado { get; set; }
        
        public decimal ValorCaucao { get; set; }
        
        public string? Observacoes { get; set; }
    }
}
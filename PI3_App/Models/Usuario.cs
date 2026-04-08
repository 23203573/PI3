using Microsoft.AspNetCore.Identity;

namespace PensionatoApp.Models
{
    public class Usuario : IdentityUser
    {
        public string? NomeCompleto { get; set; }
        public DateTime DataCadastro { get; set; } = DateTime.Now;
        public bool Ativo { get; set; } = true;
    }
}
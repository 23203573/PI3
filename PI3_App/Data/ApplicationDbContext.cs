using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PensionatoApp.Models;

namespace PensionatoApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<Usuario>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Suite> Suites { get; set; }
        public DbSet<Hospede> Hospedes { get; set; }
        public DbSet<Reserva> Reservas { get; set; }
        public DbSet<Pagamento> Pagamentos { get; set; }
        public DbSet<RegistroLimpeza> RegistrosLimpeza { get; set; }
        public DbSet<RegistroManutencao> RegistrosManutencao { get; set; }
        public DbSet<Notificacao> Notificacoes { get; set; }
        public DbSet<ReceitaMensal> ReceitasMensais { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configurações de relacionamentos
            builder.Entity<Reserva>()
                .HasOne(r => r.Suite)
                .WithMany(s => s.Reservas)
                .HasForeignKey(r => r.SuiteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Reserva>()
                .HasOne(r => r.Hospede)
                .WithMany(h => h.Reservas)
                .HasForeignKey(r => r.HospedeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Pagamento>()
                .HasOne(p => p.Reserva)
                .WithMany(r => r.Pagamentos)
                .HasForeignKey(p => p.ReservaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RegistroLimpeza>()
                .HasOne(rl => rl.Suite)
                .WithMany(s => s.RegistrosLimpeza)
                .HasForeignKey(rl => rl.SuiteId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RegistroManutencao>()
                .HasOne(rm => rm.Suite)
                .WithMany(s => s.RegistrosManutencao)
                .HasForeignKey(rm => rm.SuiteId)
                .OnDelete(DeleteBehavior.Cascade);

            // Dados iniciais das suítes
            builder.Entity<Suite>().HasData(
                new Suite { Id = 1, Numero = 1, PrecoMensal = 1700.00m, TipoCama = TipoBed.Casal },
                new Suite { Id = 2, Numero = 2, PrecoMensal = 1600.00m, TipoCama = TipoBed.Solteiro },
                new Suite { Id = 3, Numero = 3, PrecoMensal = 1750.00m, TipoCama = TipoBed.Casal },
                new Suite { Id = 4, Numero = 4, PrecoMensal = 1750.00m, TipoCama = TipoBed.Casal },
                new Suite { Id = 5, Numero = 5, PrecoMensal = 1900.00m, TipoCama = TipoBed.Casal },
                new Suite { Id = 6, Numero = 6, PrecoMensal = 1700.00m, TipoCama = TipoBed.Casal },
                new Suite { Id = 7, Numero = 7, PrecoMensal = 1700.00m, TipoCama = TipoBed.Casal },
                new Suite { Id = 8, Numero = 8, PrecoMensal = 1600.00m, TipoCama = TipoBed.Solteiro },
                new Suite { Id = 9, Numero = 9, PrecoMensal = 1700.00m, TipoCama = TipoBed.Casal },
                new Suite { Id = 10, Numero = 10, PrecoMensal = 1600.00m, TipoCama = TipoBed.Solteiro },
                new Suite { Id = 11, Numero = 11, PrecoMensal = 1700.00m, TipoCama = TipoBed.Casal }
            );
        }
    }
}
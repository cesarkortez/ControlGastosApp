using ControlGastosApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ControlGastosApp.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

            public DbSet<TipoGasto> TiposGasto { get; set; }
            public DbSet<FondoMonetario> FondosMonetarios { get; set; }
            public DbSet<Presupuesto> Presupuestos { get; set; }
            public DbSet<Gasto> Gastos { get; set; }
            public DbSet<DetalleGasto> DetallesGasto { get; set; }
            public DbSet<Deposito> Depositos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de relaciones
            modelBuilder.Entity<Gasto>()
            .HasMany(g => g.Detalles)
            .WithOne(d => d.Gasto)
            .HasForeignKey(d => d.GastoId)  // Cambiado de Id a GastoId
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Gasto>()
                .HasIndex(g => new { g.TipoDocumento, g.NumeroDocumento })
                .IsUnique();

            modelBuilder.Entity<Presupuesto>()
                .HasIndex(p => new { p.UserId, p.TipoGastoCodigo, p.Mes, p.Anio })
                .IsUnique();
        }
        
    }
}

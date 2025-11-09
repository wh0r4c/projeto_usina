using Microsoft.EntityFrameworkCore;
using UsinaApi.Models;

namespace UsinaApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Holerite> Holerites { get; set; }

        public DbSet<GravacaoRh> GravacoesRh { get; set; }

        public DbSet<Aviso> Avisos { get; set; }

        public DbSet<Faq> Faqs { get; set; }

        public DbSet<BancoHoras> BancoHoras { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Garante que CPF é único
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Cpf)
                .IsUnique();
        }
    }
}
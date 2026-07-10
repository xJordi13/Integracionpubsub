using ApiFacturacion.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiFacturacion.Data;

public sealed class FacturacionDbContext(DbContextOptions<FacturacionDbContext> options) : DbContext(options)
{
    public DbSet<ProductoFacturado> Productos => Set<ProductoFacturado>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductoFacturado>(entity =>
        {
            entity.ToTable("Productos");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ProductoOrdenId).IsUnique();
            entity.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
        });
    }
}

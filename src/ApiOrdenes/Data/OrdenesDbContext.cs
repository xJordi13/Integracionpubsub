using ApiOrdenes.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiOrdenes.Data;

public sealed class OrdenesDbContext(DbContextOptions<OrdenesDbContext> options) : DbContext(options)
{
    public DbSet<ProductoOrden> Productos => Set<ProductoOrden>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductoOrden>(entity =>
        {
            entity.ToTable("Productos");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Estado).HasConversion<string>().HasMaxLength(30);
        });
    }
}

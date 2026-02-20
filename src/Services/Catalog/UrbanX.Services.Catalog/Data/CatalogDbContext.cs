using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Catalog.Models;

namespace UrbanX.Services.Catalog.Data;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.MerchantId).IsRequired();
            entity.HasIndex(e => e.MerchantId);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Payload).IsRequired();
            entity.HasIndex(e => e.ProcessedAt);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}

using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Merchant.Models;

namespace UrbanX.Services.Merchant.Data;

public class MerchantDbContext : DbContext
{
    public MerchantDbContext(DbContextOptions<MerchantDbContext> options) : base(options)
    {
    }

    public DbSet<Models.Merchant> Merchants => Set<Models.Merchant>();
    public DbSet<MerchantProduct> Products => Set<MerchantProduct>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Models.Merchant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<MerchantProduct>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.HasIndex(e => e.MerchantId);
        });
    }
}

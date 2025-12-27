using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Payment.Models;

namespace UrbanX.Services.Payment.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<Models.Payment> Payments => Set<Models.Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Models.Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.TransactionId);
        });
    }
}

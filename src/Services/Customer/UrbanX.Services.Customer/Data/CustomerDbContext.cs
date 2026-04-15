using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Customer.Models;

namespace UrbanX.Services.Customer.Data;

public class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options) : base(options)
    {
    }

    public DbSet<Models.Customer> Customers => Set<Models.Customer>();
    public DbSet<CustomerGroup> CustomerGroups => Set<CustomerGroup>();
    public DbSet<CustomerGroupMembership> CustomerGroupMemberships => Set<CustomerGroupMembership>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Models.Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<CustomerGroup>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<CustomerGroupMembership>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CustomerId, e.CustomerGroupId }).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.CustomerGroupId);
        });
    }
}

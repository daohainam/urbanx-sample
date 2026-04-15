using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Customer.Data;
using UrbanX.Services.Customer.Models;

namespace UrbanX.Services.Customer.IntegrationTests;

public class CustomerServiceIntegrationTests
{
    [Fact]
    public async Task CustomerService_ShouldManageCustomers()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseInMemoryDatabase(databaseName: "CustomerIntegrationTest_" + Guid.NewGuid())
            .Options;

        using var context = new CustomerDbContext(options);

        var customer = new Models.Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "Alice",
            LastName = "Johnson",
            Email = "alice@example.com",
            Phone = "+1-555-0101",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.Customers.FindAsync(customer.Id);
        Assert.NotNull(saved);
        Assert.Equal("Alice", saved!.FirstName);
        Assert.Equal("alice@example.com", saved.Email);
    }

    [Fact]
    public async Task CustomerService_ShouldManageCustomerGroups()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseInMemoryDatabase(databaseName: "CustomerIntegrationTest_" + Guid.NewGuid())
            .Options;

        using var context = new CustomerDbContext(options);

        var groups = new List<CustomerGroup>
        {
            new() {
                Id = Guid.NewGuid(),
                Name = "Standard",
                Description = "Standard customers",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.NewGuid(),
                Name = "VIP",
                Description = "VIP customers",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.NewGuid(),
                Name = "Premium",
                Description = "Premium customers",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act
        context.CustomerGroups.AddRange(groups);
        await context.SaveChangesAsync();

        // Assert
        var allGroups = await context.CustomerGroups.ToListAsync();
        Assert.Equal(3, allGroups.Count);
    }

    [Fact]
    public async Task CustomerService_ShouldAssignCustomerToGroup()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseInMemoryDatabase(databaseName: "CustomerIntegrationTest_" + Guid.NewGuid())
            .Options;

        using var context = new CustomerDbContext(options);

        var customerId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var customer = new Models.Customer
        {
            Id = customerId,
            FirstName = "Bob",
            LastName = "Smith",
            Email = "bob@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var group = new CustomerGroup
        {
            Id = groupId,
            Name = "VIP",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var membership = new CustomerGroupMembership
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            CustomerGroupId = groupId,
            JoinedAt = DateTime.UtcNow
        };

        // Act
        context.Customers.Add(customer);
        context.CustomerGroups.Add(group);
        context.CustomerGroupMemberships.Add(membership);
        await context.SaveChangesAsync();

        // Assert
        var memberships = await context.CustomerGroupMemberships
            .Where(m => m.CustomerId == customerId)
            .ToListAsync();
        Assert.Single(memberships);
        Assert.Equal(groupId, memberships.First().CustomerGroupId);
    }

    [Fact]
    public async Task CustomerService_ShouldDeactivateCustomer()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseInMemoryDatabase(databaseName: "CustomerIntegrationTest_" + Guid.NewGuid())
            .Options;

        using var context = new CustomerDbContext(options);

        var customer = new Models.Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "Charlie",
            LastName = "Brown",
            Email = "charlie@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        // Act - Deactivate
        customer.IsActive = false;
        customer.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Assert
        var updated = await context.Customers.FindAsync(customer.Id);
        Assert.False(updated!.IsActive);
    }

    [Fact]
    public async Task CustomerService_ShouldUpdateCustomer()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseInMemoryDatabase(databaseName: "CustomerIntegrationTest_" + Guid.NewGuid())
            .Options;

        using var context = new CustomerDbContext(options);

        var customer = new Models.Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "Diana",
            LastName = "Prince",
            Email = "diana@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        // Act - Update phone
        customer.Phone = "+1-999-0001";
        customer.City = "Metropolis";
        customer.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Assert
        var updated = await context.Customers.FindAsync(customer.Id);
        Assert.Equal("+1-999-0001", updated!.Phone);
        Assert.Equal("Metropolis", updated.City);
    }

    [Fact]
    public async Task CustomerService_ShouldRemoveMembershipFromGroup()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseInMemoryDatabase(databaseName: "CustomerIntegrationTest_" + Guid.NewGuid())
            .Options;

        using var context = new CustomerDbContext(options);

        var customerId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var membership = new CustomerGroupMembership
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            CustomerGroupId = groupId,
            JoinedAt = DateTime.UtcNow
        };

        context.CustomerGroupMemberships.Add(membership);
        await context.SaveChangesAsync();

        // Act - Remove membership
        context.CustomerGroupMemberships.Remove(membership);
        await context.SaveChangesAsync();

        // Assert
        var memberships = await context.CustomerGroupMemberships
            .Where(m => m.CustomerId == customerId && m.CustomerGroupId == groupId)
            .ToListAsync();
        Assert.Empty(memberships);
    }
}

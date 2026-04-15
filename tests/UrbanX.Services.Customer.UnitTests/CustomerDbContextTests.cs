using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Customer.Data;
using UrbanX.Services.Customer.Models;

namespace UrbanX.Services.Customer.UnitTests;

public class CustomerDbContextTests
{
    [Fact]
    public void CustomerDbContext_ShouldConfigureEntities()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseInMemoryDatabase(databaseName: "CustomerTestDb_" + Guid.NewGuid())
            .Options;

        // Act
        using var context = new CustomerDbContext(options);
        var customerEntityType = context.Model.FindEntityType(typeof(Models.Customer));
        var groupEntityType = context.Model.FindEntityType(typeof(CustomerGroup));
        var membershipEntityType = context.Model.FindEntityType(typeof(CustomerGroupMembership));

        // Assert
        Assert.NotNull(customerEntityType);
        Assert.NotNull(groupEntityType);
        Assert.NotNull(membershipEntityType);
    }

    [Fact]
    public async Task CustomerDbContext_ShouldAddAndRetrieveCustomer()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseInMemoryDatabase(databaseName: "CustomerTestDb_" + Guid.NewGuid())
            .Options;

        var customer = new Models.Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "Alice",
            LastName = "Johnson",
            Email = "alice@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        using (var context = new CustomerDbContext(options))
        {
            context.Customers.Add(customer);
            await context.SaveChangesAsync();
        }

        // Assert
        using (var context = new CustomerDbContext(options))
        {
            var saved = await context.Customers.FindAsync(customer.Id);
            Assert.NotNull(saved);
            Assert.Equal("Alice", saved!.FirstName);
            Assert.Equal("alice@example.com", saved.Email);
        }
    }

    [Fact]
    public async Task CustomerDbContext_ShouldAddAndRetrieveCustomerGroup()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseInMemoryDatabase(databaseName: "CustomerTestDb_" + Guid.NewGuid())
            .Options;

        var group = new CustomerGroup
        {
            Id = Guid.NewGuid(),
            Name = "VIP",
            Description = "VIP customers",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        using (var context = new CustomerDbContext(options))
        {
            context.CustomerGroups.Add(group);
            await context.SaveChangesAsync();
        }

        // Assert
        using (var context = new CustomerDbContext(options))
        {
            var saved = await context.CustomerGroups.FindAsync(group.Id);
            Assert.NotNull(saved);
            Assert.Equal("VIP", saved!.Name);
        }
    }

    [Fact]
    public async Task CustomerDbContext_ShouldAddAndRetrieveMembership()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseInMemoryDatabase(databaseName: "CustomerTestDb_" + Guid.NewGuid())
            .Options;

        var customerId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var membership = new CustomerGroupMembership
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            CustomerGroupId = groupId,
            JoinedAt = DateTime.UtcNow
        };

        // Act
        using (var context = new CustomerDbContext(options))
        {
            context.CustomerGroupMemberships.Add(membership);
            await context.SaveChangesAsync();
        }

        // Assert
        using (var context = new CustomerDbContext(options))
        {
            var saved = await context.CustomerGroupMemberships.FindAsync(membership.Id);
            Assert.NotNull(saved);
            Assert.Equal(customerId, saved!.CustomerId);
            Assert.Equal(groupId, saved.CustomerGroupId);
        }
    }

    [Fact]
    public async Task CustomerDbContext_ShouldFilterMembershipsByCustomer()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseInMemoryDatabase(databaseName: "CustomerTestDb_" + Guid.NewGuid())
            .Options;

        var customerId = Guid.NewGuid();
        var membership1 = new CustomerGroupMembership
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            CustomerGroupId = Guid.NewGuid(),
            JoinedAt = DateTime.UtcNow
        };
        var membership2 = new CustomerGroupMembership
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            CustomerGroupId = Guid.NewGuid(),
            JoinedAt = DateTime.UtcNow
        };

        // Act
        using (var context = new CustomerDbContext(options))
        {
            context.CustomerGroupMemberships.AddRange(membership1, membership2);
            await context.SaveChangesAsync();
        }

        // Assert
        using (var context = new CustomerDbContext(options))
        {
            var memberships = await context.CustomerGroupMemberships
                .Where(m => m.CustomerId == customerId)
                .ToListAsync();
            Assert.Single(memberships);
        }
    }
}

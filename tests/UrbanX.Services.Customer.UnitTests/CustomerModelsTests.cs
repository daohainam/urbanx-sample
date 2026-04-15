using UrbanX.Services.Customer.Models;

namespace UrbanX.Services.Customer.UnitTests;

public class CustomerModelsTests
{
    [Fact]
    public void Customer_ShouldInitializeWithRequiredProperties()
    {
        // Arrange & Act
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

        // Assert
        Assert.NotNull(customer);
        Assert.Equal("Alice", customer.FirstName);
        Assert.Equal("Johnson", customer.LastName);
        Assert.Equal("alice@example.com", customer.Email);
        Assert.True(customer.IsActive);
    }

    [Fact]
    public void Customer_ShouldAllowOptionalProperties()
    {
        // Arrange & Act
        var customer = new Models.Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "Bob",
            LastName = "Smith",
            Email = "bob@example.com",
            Phone = "+1-555-0202",
            Address = "200 Oak Avenue",
            City = "Los Angeles",
            Country = "US",
            PostalCode = "90001",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal("+1-555-0202", customer.Phone);
        Assert.Equal("200 Oak Avenue", customer.Address);
        Assert.Equal("Los Angeles", customer.City);
        Assert.Equal("US", customer.Country);
        Assert.Equal("90001", customer.PostalCode);
    }

    [Fact]
    public void CustomerGroup_ShouldInitializeWithRequiredProperties()
    {
        // Arrange & Act
        var group = new CustomerGroup
        {
            Id = Guid.NewGuid(),
            Name = "VIP",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.NotNull(group);
        Assert.Equal("VIP", group.Name);
        Assert.True(group.IsActive);
    }

    [Fact]
    public void CustomerGroup_ShouldAllowOptionalDescription()
    {
        // Arrange & Act
        var group = new CustomerGroup
        {
            Id = Guid.NewGuid(),
            Name = "Premium",
            Description = "Premium tier customers with exclusive benefits",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal("Premium tier customers with exclusive benefits", group.Description);
    }

    [Fact]
    public void CustomerGroupMembership_ShouldInitializeWithRequiredProperties()
    {
        // Arrange & Act
        var customerId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var membership = new CustomerGroupMembership
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            CustomerGroupId = groupId,
            JoinedAt = DateTime.UtcNow
        };

        // Assert
        Assert.NotNull(membership);
        Assert.Equal(customerId, membership.CustomerId);
        Assert.Equal(groupId, membership.CustomerGroupId);
    }
}

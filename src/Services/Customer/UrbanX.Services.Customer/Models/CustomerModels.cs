namespace UrbanX.Services.Customer.Models;

public class Customer
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CustomerGroup
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CustomerGroupMembership
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid CustomerGroupId { get; set; }
    public DateTime JoinedAt { get; set; }
}

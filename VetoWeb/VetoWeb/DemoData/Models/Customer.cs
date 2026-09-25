namespace VetoWeb.DemoData.Models;

public class Customer : IEntity
{
    public int ID { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string Email { get; set; } = default!;
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public DateOnly CustomerSince { get; set; }
    public CustomerTier Tier { get; set; }
    public decimal TotalSpent { get; set; }
    public bool IsSubscribed { get; set; }
    public Location Location { get; set; } = default!;
}

public enum CustomerTier
{
    Standard,
    Silver,
    Gold,
    Platinum
}
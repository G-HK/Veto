namespace VetoWeb.DemoData.Models;

public class Customer : User
{
    public DateOnly? DateOfBirth { get; set; }
    public DateOnly CustomerSince { get; set; }
    public CustomerTier Tier { get; set; }
    public decimal TotalSpent { get; set; }
    public bool IsSubscribed { get; set; }
}

public enum CustomerTier
{
    Standard,
    Silver,
    Gold,
    Platinum
}
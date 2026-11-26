using System.Text.Json.Serialization;

namespace VetoWeb.Client.DemoData.Models;

public class Customer : User
{
    public int DealerID { get; set; }
    [JsonIgnore]
    public Dealer Dealer { get; set; } = default!;

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
using System.Text.Json.Serialization;

namespace VetoWeb.Client.DemoData.Models;

public class Employee : User
{
    public string JobTitle { get; set; } = default!;
    public string Department { get; set; } = default!;
    public DateOnly HireDate { get; set; }
    public decimal Salary { get; set; }
    public int DealerID { get; set; }
    [JsonIgnore]
    public Dealer Dealer { get; set; } = default!;
    public int? ManagerID { get; set; }
    public int DistanceToWork { get; set; }
}
namespace VetoWeb.DemoData.Models;

public class Employee : IEntity
{
    public int ID { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string JobTitle { get; set; } = default!;
    public string Department { get; set; } = default!;
    public DateOnly HireDate { get; set; }
    public decimal Salary { get; set; }
    public bool IsActive { get; set; }
    public int DealerID { get; set; }
    public int? ManagerID { get; set; }
    public int DistanceToWork { get; set; }
    public Location Location { get; set; } = new();
}

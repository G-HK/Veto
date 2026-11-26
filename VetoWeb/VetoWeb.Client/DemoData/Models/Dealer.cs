namespace VetoWeb.Client.DemoData.Models;

public class Dealer : IEntity
{
    public int ID { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string Phone { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Website { get; set; }
    public double Rating { get; set; }
    public DateOnly OpenedOn { get; set; }
    public bool IsActive { get; set; }
    public Location Location { get; set; } = new();

    public IReadOnlyList<Car> Cars { get; init; } = [];
    public IReadOnlyList<Employee> Employees { get; init; } = [];
    public IReadOnlyList<Customer> Customers { get; init; } = [];
}
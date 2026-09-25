namespace VetoWeb.DemoData.Models;

// Find Dummy Database for Testing/Demo Enviromented.


public interface IEntity
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; } // soft delete: null = not deleted
}

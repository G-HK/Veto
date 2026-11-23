namespace VetoWeb.Client.DemoData.Models;

/// <summary>Anyone with an account. <see cref="Customer"/> and <see cref="Employee"/> derive from it.</summary>
public class User : IEntity
{
    public int ID { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string UserName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public UserRole Role { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
    public Location Location { get; set; } = new();
}

public enum UserRole
{
    Viewer,
    Editor,
    Admin
}
namespace VetoWeb.DemoData.Models;

public class Location
{
    public string Street { get; set; } = default!;
    public string City { get; set; } = default!;
    public string? Region { get; set; }
    public string PostalCode { get; set; } = default!;
    public string Country { get; set; } = default!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
using System.Text.Json;
using VetoWeb.DemoData.Models;

namespace VetoWeb.DemoData;

public sealed class Data
{
    private static readonly Lazy<Db> Db = new(() =>
    {
        using var stream = typeof(Data).Assembly.GetManifestResourceStream("db.json")!;
        return JsonSerializer.Deserialize<Db>(stream, JsonSerializerOptions.Web)!;
    });

    public static Db Get() => Db.Value;
}


public sealed class Db
{
    public List<Dealer> Dealers { get; set; } = [];
    public List<Car> Cars { get; set; } = [];
    public List<Customer> Customers { get; set; } = [];
    public List<Employee> Employees { get; set; } = [];
}
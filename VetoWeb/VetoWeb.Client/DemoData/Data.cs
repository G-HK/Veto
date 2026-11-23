using System.Text.Json;
using System.Text.Json.Serialization;
using VetoWeb.Client.DemoData.Models;

namespace VetoWeb.Client.DemoData;

public static class Data
{
    private const string ResourceName = "db.json";
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly Lazy<Db> Instance = new(Load);

    public static Db Get() => Instance.Value;

    private static Db Load()
    {
        using var stream = typeof(Data).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"The embedded resource '{ResourceName}' is missing. Check the EmbeddedResource item in VetoWeb.Client.csproj.");

        var db = JsonSerializer.Deserialize<Db>(stream, Options)
            ?? throw new InvalidOperationException($"'{ResourceName}' is empty.");

        return Link(db);
    }

    private static Db Link(Db db)
    {
        foreach (var dealer in db.Dealers)
        {
            foreach (var car in dealer.Cars) { car.Dealer = dealer; car.DealerID = dealer.ID; }
            foreach (var employee in dealer.Employees) { employee.Dealer = dealer; employee.DealerID = dealer.ID; }
            foreach (var customer in dealer.Customers) { customer.Dealer = dealer; customer.DealerID = dealer.ID; }
        }

        // The all-dealer lists in ID order, not dealer by dealer - car 1 first, car 1000 last.
        db.Cars = [.. db.Dealers.SelectMany(d => d.Cars).OrderBy(c => c.ID)];
        db.Employees = [.. db.Dealers.SelectMany(d => d.Employees).OrderBy(e => e.ID)];
        db.Customers = [.. db.Dealers.SelectMany(d => d.Customers).OrderBy(c => c.ID)];
        return db;
    }
}

public sealed class Db
{
    public IReadOnlyList<Dealer> Dealers { get; init; } = [];

    [JsonIgnore]
    public IReadOnlyList<Car> Cars { get; internal set; } = [];
    [JsonIgnore]
    public IReadOnlyList<Employee> Employees { get; internal set; } = [];
    [JsonIgnore]
    public IReadOnlyList<Customer> Customers { get; internal set; } = [];
}
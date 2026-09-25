namespace VetoWeb.DemoData.Models;

public class Car : IEntity
{
    public int ID { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }            // soft delete: null = not deleted
    public string Make { get; set; } = default!;
    public string Model { get; set; } = default!;
    public int BuildYear { get; set; }
    public string? Trim { get; set; }
    public string? ChassisCode { get; set; }           // e.g. BMW "E46", "E92", "G20"; Mercedes "W204"; VW "Mk7"
    public bool IsFacelift { get; set; }               // BMW "LCI"
    public BodyType BodyType { get; set; }
    public int Doors { get; set; }
    public int Seats { get; set; }
    public string Color { get; set; } = default!;
    public string? InteriorColor { get; set; }

    // Powertrain
    public string? EngineCode { get; set; }            // e.g. BMW "M54B30", "B58B30", "S55"; null for electric
    public string? TransmissionCode { get; set; }      // e.g. "ZF 8HP45", "Getrag 420G"
    public FuelType FuelType { get; set; }
    public Transmission Transmission { get; set; }
    public int Gears { get; set; }
    public Drivetrain Drivetrain { get; set; }
    public EngineLayout EngineLayout { get; set; }
    public double? EngineDisplacementL { get; set; }   // null for electric
    public int? Cylinders { get; set; }                // null for electric
    public bool IsTurbo { get; set; }
    public int HorsePower { get; set; }
    public int TorqueNm { get; set; }
    public int? BatteryCapacityKWh { get; set; }       // electric / hybrid only
    public int? ElectricRangeKm { get; set; }          // electric / hybrid only

    // Performance & economy
    public double ZeroToHundredSec { get; set; }
    public int TopSpeedKmh { get; set; }
    public double? FuelConsumptionL100Km { get; set; } // null for electric
    public int Co2GramsPerKm { get; set; }

    // Dimensions
    public int LengthMm { get; set; }
    public int WidthMm { get; set; }
    public int HeightMm { get; set; }
    public int WheelbaseMm { get; set; }
    public int CurbWeightKg { get; set; }
    public int TrunkVolumeL { get; set; }
    public int WheelSizeInch { get; set; }

    // Listing
    public CarCondition Condition { get; set; }
    public int Mileage { get; set; }
    public int PreviousOwners { get; set; }
    public DateOnly? FirstRegistration { get; set; }
    public decimal Price { get; set; }
    public string Vin { get; set; } = default!;
    public string? LicensePlate { get; set; }
    public bool IsNew { get; set; }
    public bool IsSold { get; set; }
    public List<string> Features { get; set; } = [];   // e.g. "Sunroof", "Heated Seats"
    public string? ImageUrl { get; set; }
    public int DealerID { get; set; }
}

public enum FuelType { Petrol, Diesel, Hybrid, Electric }

public enum Transmission { Manual, Automatic, SemiAutomatic, CVT, DualClutch }

public enum Drivetrain { FWD, RWD, AWD, FourWD }

public enum BodyType { Sedan, Hatchback, Wagon, Coupe, Convertible, SUV, Crossover, Pickup, Van, Minivan }

public enum EngineLayout { Front, Mid, Rear }

public enum CarCondition { New, Used, CertifiedPreOwned, Occassic }
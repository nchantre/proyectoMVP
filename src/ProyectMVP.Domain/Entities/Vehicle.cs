namespace ProyectMVP.Domain.Entities;

public sealed class Vehicle
{
    public long VehicleId { get; set; }
    public string Plate { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? VehicleClass { get; set; }
    public string? VehicleLine { get; set; }
    public string? Color { get; set; }
    public int? ModelYear { get; set; }
}

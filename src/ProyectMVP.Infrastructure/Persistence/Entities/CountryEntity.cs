namespace ProyectMVP.Infrastructure.Persistence.Entities;

public sealed class CountryEntity
{
    public int CountryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IsoCode { get; set; } = string.Empty;
}

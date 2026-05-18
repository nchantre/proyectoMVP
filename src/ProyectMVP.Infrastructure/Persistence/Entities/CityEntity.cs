namespace ProyectMVP.Infrastructure.Persistence.Entities;

public sealed class CityEntity
{
    public int CityId { get; set; }
    public int CountryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CountryEntity? Country { get; set; }
}

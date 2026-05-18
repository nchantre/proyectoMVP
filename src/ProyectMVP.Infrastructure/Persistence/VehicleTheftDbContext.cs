using Microsoft.EntityFrameworkCore;
using ProyectMVP.Infrastructure.Persistence.Entities;

namespace ProyectMVP.Infrastructure.Persistence;

public sealed class VehicleTheftDbContext : DbContext
{
    public VehicleTheftDbContext(DbContextOptions<VehicleTheftDbContext> options)
        : base(options)
    {
    }

    public DbSet<CountryEntity> Countries => Set<CountryEntity>();
    public DbSet<CityEntity> Cities => Set<CityEntity>();
    public DbSet<DeviceEntity> Devices => Set<DeviceEntity>();
    public DbSet<VehicleEntity> Vehicles => Set<VehicleEntity>();
    public DbSet<StolenVehicleReportEntity> StolenVehicleReports => Set<StolenVehicleReportEntity>();
    public DbSet<SightingEntity> Sightings => Set<SightingEntity>();
    public DbSet<SightingEvidenceEntity> SightingEvidences => Set<SightingEvidenceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CountryEntity>(e =>
        {
            e.ToTable("Country");
            e.HasKey(x => x.CountryId);
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.IsoCode).HasMaxLength(2).IsFixedLength().IsRequired();
            e.HasIndex(x => x.IsoCode).IsUnique();
        });

        modelBuilder.Entity<CityEntity>(e =>
        {
            e.ToTable("City");
            e.HasKey(x => x.CityId);
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.HasOne(x => x.Country).WithMany().HasForeignKey(x => x.CountryId);
            e.HasIndex(x => new { x.CountryId, x.Name });
        });

        modelBuilder.Entity<DeviceEntity>(e =>
        {
            e.ToTable("Device");
            e.HasKey(x => x.DeviceId);
            e.Property(x => x.SerialNumber).HasMaxLength(100).IsRequired();
            e.Property(x => x.ApiKeyHash).HasMaxLength(256).IsRequired();
            e.Property(x => x.Latitude).HasPrecision(9, 6);
            e.Property(x => x.Longitude).HasPrecision(9, 6);
            e.HasIndex(x => x.SerialNumber).IsUnique();
        });

        modelBuilder.Entity<VehicleEntity>(e =>
        {
            e.ToTable("Vehicle");
            e.HasKey(x => x.VehicleId);
            e.Property(x => x.Plate).HasMaxLength(20).IsRequired();
            e.Property(x => x.Brand).HasMaxLength(80);
            e.Property(x => x.VehicleClass).HasMaxLength(80);
            e.Property(x => x.VehicleLine).HasMaxLength(80);
            e.Property(x => x.Color).HasMaxLength(50);
            e.HasIndex(x => x.Plate).IsUnique();
        });

        modelBuilder.Entity<StolenVehicleReportEntity>(e =>
        {
            e.ToTable("StolenVehicleReport");
            e.HasKey(x => x.StolenVehicleReportId);
            e.Property(x => x.OwnerDocument).HasMaxLength(40).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.SourceSystem).HasMaxLength(120);
            e.HasIndex(x => new { x.VehicleId, x.Status });
            e.HasIndex(x => new { x.CountryId, x.Status });
        });

        modelBuilder.Entity<SightingEntity>(e =>
        {
            e.ToTable("Sighting");
            e.HasKey(x => x.SightingId);
            e.Property(x => x.Latitude).HasPrecision(9, 6);
            e.Property(x => x.Longitude).HasPrecision(9, 6);
            e.Property(x => x.Confidence).HasPrecision(5, 2);
            e.HasIndex(x => new { x.VehicleId, x.SeenAtUtc });
            e.HasIndex(x => x.SeenAtUtc);
            e.HasIndex(x => new { x.CountryId, x.CityId, x.SeenAtUtc });
        });

        modelBuilder.Entity<SightingEvidenceEntity>(e =>
        {
            e.ToTable("SightingEvidence");
            e.HasKey(x => x.SightingEvidenceId);
            e.Property(x => x.EvidenceType).HasMaxLength(20).IsRequired();
            e.Property(x => x.StorageUrl).HasMaxLength(500).IsRequired();
            e.Property(x => x.Sha256).HasMaxLength(64).IsFixedLength();
            e.HasOne(x => x.Sighting).WithMany(x => x.Evidences).HasForeignKey(x => x.SightingId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

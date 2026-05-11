using AgroGuide.FertilizerApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroGuide.FertilizerApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Crop> Crops => Set<Crop>();
    public DbSet<SoilAdjustment> SoilAdjustments => Set<SoilAdjustment>();
    public DbSet<Fertilizer> Fertilizers => Set<Fertilizer>();
    public DbSet<ApplicationSchedule> ApplicationSchedules => Set<ApplicationSchedule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationSchedule>(entity =>
        {
            entity.HasOne(e => e.Crop)
                .WithMany(c => c.ApplicationSchedules)
                .HasForeignKey(e => e.CropId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Crop>().HasData(
            new Crop { Id = 1, Name = "Wheat", NPerTon = 25, PPerTon = 12, KPerTon = 18 },
            new Crop { Id = 2, Name = "Maize", NPerTon = 22, PPerTon = 10, KPerTon = 20 },
            new Crop { Id = 3, Name = "Rice", NPerTon = 18, PPerTon = 9, KPerTon = 16 });

        modelBuilder.Entity<SoilAdjustment>().HasData(
            new SoilAdjustment { Id = 1, Nutrient = "N", SoilLevel = "Low", ReductionPercent = 0 },
            new SoilAdjustment { Id = 2, Nutrient = "N", SoilLevel = "Medium", ReductionPercent = 12 },
            new SoilAdjustment { Id = 3, Nutrient = "N", SoilLevel = "High", ReductionPercent = 28 },
            new SoilAdjustment { Id = 4, Nutrient = "P", SoilLevel = "Low", ReductionPercent = 0 },
            new SoilAdjustment { Id = 5, Nutrient = "P", SoilLevel = "Medium", ReductionPercent = 15 },
            new SoilAdjustment { Id = 6, Nutrient = "P", SoilLevel = "High", ReductionPercent = 32 },
            new SoilAdjustment { Id = 7, Nutrient = "K", SoilLevel = "Low", ReductionPercent = 0 },
            new SoilAdjustment { Id = 8, Nutrient = "K", SoilLevel = "Medium", ReductionPercent = 12 },
            new SoilAdjustment { Id = 9, Nutrient = "K", SoilLevel = "High", ReductionPercent = 30 });

        modelBuilder.Entity<Fertilizer>().HasData(
            new Fertilizer { Id = 1, Name = "Urea", NPercent = 46, PPercent = 0, KPercent = 0 },
            new Fertilizer { Id = 2, Name = "DAP", NPercent = 18, PPercent = 46, KPercent = 0 },
            new Fertilizer { Id = 3, Name = "MOP", NPercent = 0, PPercent = 0, KPercent = 60 });

        modelBuilder.Entity<ApplicationSchedule>().HasData(
            new ApplicationSchedule
            {
                Id = 1,
                CropId = 1,
                Stage = "Basal",
                Timing = "At sowing",
                NutrientsToApply = "Full DAP, ~30% of Urea, ~40% of MOP"
            },
            new ApplicationSchedule
            {
                Id = 2,
                CropId = 1,
                Stage = "Top dress",
                Timing = "Tillering to stem elongation",
                NutrientsToApply = "Remaining Urea and MOP"
            },
            new ApplicationSchedule
            {
                Id = 3,
                CropId = 2,
                Stage = "Basal",
                Timing = "At planting",
                NutrientsToApply = "Full DAP, ~25% of Urea, ~35% of MOP"
            },
            new ApplicationSchedule
            {
                Id = 4,
                CropId = 2,
                Stage = "Top dress",
                Timing = "V8–V10 growth stage",
                NutrientsToApply = "Remaining Urea and MOP"
            },
            new ApplicationSchedule
            {
                Id = 5,
                CropId = 3,
                Stage = "Basal",
                Timing = "Before transplant or at permanent flood",
                NutrientsToApply = "Full DAP, ~30% of Urea, ~40% of MOP"
            },
            new ApplicationSchedule
            {
                Id = 6,
                CropId = 3,
                Stage = "Top dress",
                Timing = "Panicle initiation",
                NutrientsToApply = "Remaining Urea and MOP"
            });
    }
}

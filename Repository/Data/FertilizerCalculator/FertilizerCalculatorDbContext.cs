using Domain.Models.FertilizerCalculator;
using Microsoft.EntityFrameworkCore;

namespace Repository.Data;

public class FertilizerCalculatorDbContext : DbContext
{
    public FertilizerCalculatorDbContext(DbContextOptions<FertilizerCalculatorDbContext> options) : base(options)
    {
    }

    public DbSet<CropRequirements> CropRequirements { get; set; }
    public DbSet<Fertilizer> Fertilizers { get; set; }
    public DbSet<SoilMultiplier> SoilMultiplier { get; set; }
}
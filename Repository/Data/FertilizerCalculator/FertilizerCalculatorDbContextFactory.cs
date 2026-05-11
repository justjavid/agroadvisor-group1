using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Repository.Data;

public class FertilizerCalculatorDbContextFactory : IDesignTimeDbContextFactory<FertilizerCalculatorDbContext>

{
    public FertilizerCalculatorDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FertilizerCalculatorDbContext>();

        optionsBuilder.UseNpgsql("Host=localhost;Port=5454;Database=agro_advisor;Username=postgres;Password=postgres");

        return new FertilizerCalculatorDbContext(optionsBuilder.Options);
    }
}
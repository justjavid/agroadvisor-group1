using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Repository.Data.FertilizerCalculator;

public class FertilizerCalculatorDbContextFactory : IDesignTimeDbContextFactory<FertilizerCalculatorDbContext>
{
    public FertilizerCalculatorDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FertilizerCalculatorDbContext>();
        optionsBuilder.UseSqlite("Data Source=fertilizercalculator.db");
        return new FertilizerCalculatorDbContext(optionsBuilder.Options);
    }
}
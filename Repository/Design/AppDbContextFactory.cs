using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Repository.Design;

public class AppDbContextFactory : IDesignTimeDbContextFactory<Data.AppDbContext>
{
    public Data.AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("DefaultConnection")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=AgroAdvisor;Trusted_Connection=True;TrustServerCertificate=True";

        var optionsBuilder = new DbContextOptionsBuilder<Data.AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new Data.AppDbContext(optionsBuilder.Options);
    }
}

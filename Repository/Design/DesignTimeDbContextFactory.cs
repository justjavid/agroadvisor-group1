using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Repository.Design
{
    // Provides a design-time factory so EF tools can create the DbContext without loading the startup project assembly.
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<Data.ImageAnalysisDbContext>
    {
        public Data.ImageAnalysisDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<Data.ImageAnalysisDbContext>();
            // Fallback connection string for design-time operations. Adjust as needed for your environment.
            var connectionString = "Server=(localdb)\\mssqllocaldb;Database=ImageAnalysisDb;Trusted_Connection=True;";
            optionsBuilder.UseSqlServer(connectionString);
            return new Data.ImageAnalysisDbContext(optionsBuilder.Options);
        }
    }
}

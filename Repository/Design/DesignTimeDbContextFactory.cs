using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Repository.Data;

namespace Repository.Design
{
    // Provides a design-time factory so EF tools can create the DbContext without loading the startup project assembly.
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ChatBotDbContext>
    {
        public ChatBotDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ChatBotDbContext>();
            var connectionString = "Host=localhost;Port=5454;Database=agro_advisor;Username=postgres;Password=postgres";
            optionsBuilder.UseNpgsql(connectionString);
            return new ChatBotDbContext(optionsBuilder.Options);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Domain.Models.ImageAnalysis;

namespace Repository.Data
{
    public class ImageAnalysisDbContext : DbContext
    {
        public ImageAnalysisDbContext(DbContextOptions<ImageAnalysisDbContext> options) : base(options) { }

        public DbSet<AnalysisData> AnalysisData { get; set; }
    }
}

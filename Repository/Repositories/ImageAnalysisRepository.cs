using Domain.Models.ImageAnalysis;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class ImageAnalysisRepository : IImageAnalysisRepository
    {
        private readonly ImageAnalysisDbContext _context;
        public ImageAnalysisRepository(ImageAnalysisDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(AnalysisData entity)
        {
            await _context.AnalysisData.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

    }
}
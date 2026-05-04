using Domain.Models.ImageAnalysis;
using Repository.Data;
using Repository.Repositories.Interfaces;
using System.Linq;
using Microsoft.EntityFrameworkCore;

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

        public async Task<List<AnalysisData>> GetByUserAsync(string userId)
        {
            return await _context.AnalysisData
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();
        }

        public async Task<AnalysisData?> GetBySessionIdAsync(Guid sessionId)
        {
            return await _context.AnalysisData.FirstOrDefaultAsync(a => a.SessionId == sessionId);
        }

    }
}

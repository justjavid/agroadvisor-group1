using Domain.Models.ImageAnalysis;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories;

public class ImageAnalysisRepository : IImageAnalysisRepository
{
    private readonly AppDbContext _context;

    public ImageAnalysisRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AnalysisData entity, CancellationToken ct = default)
    {
        await _context.AnalysisData.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
    }

    public Task<List<AnalysisData>> GetByUserAsync(string userId, CancellationToken ct = default) =>
        _context.AnalysisData
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedDate)
            .ToListAsync(ct);

    public Task<AnalysisData?> GetBySessionIdAsync(Guid sessionId, CancellationToken ct = default) =>
        _context.AnalysisData.FirstOrDefaultAsync(a => a.SessionId == sessionId, ct);

    public async Task DeleteAsync(Guid sessionId, CancellationToken ct = default)
    {
        var entity = await _context.AnalysisData.FirstOrDefaultAsync(a => a.SessionId == sessionId, ct);
        if (entity is null) return;
        _context.AnalysisData.Remove(entity);
        await _context.SaveChangesAsync(ct);
    }
}

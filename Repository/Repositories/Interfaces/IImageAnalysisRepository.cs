using Domain.Models.ImageAnalysis;

namespace Repository.Repositories.Interfaces;

public interface IImageAnalysisRepository
{
    Task AddAsync(AnalysisData entity, CancellationToken ct = default);
    Task<List<AnalysisData>> GetByUserAsync(string userId, CancellationToken ct = default);
    Task<AnalysisData?> GetBySessionIdAsync(Guid sessionId, CancellationToken ct = default);
    Task DeleteAsync(Guid sessionId, CancellationToken ct = default);
}

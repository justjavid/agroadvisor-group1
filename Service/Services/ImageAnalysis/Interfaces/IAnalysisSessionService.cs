using Domain.Models.ImageAnalysis;
using Service.DTOs.ImageAnalysisDTOs;

namespace Service.Services.ImageAnalysis.Interfaces;

public interface IAnalysisSessionService
{
    Task<Guid> CreateSessionAsync(AnalyzeImageResponseDto result, string? prompt, string? userId, string? imageUrl, CancellationToken ct = default);
    Task<List<AnalysisData>> GetUserSessionsAsync(string userId, CancellationToken ct = default);
    Task<AnalysisData?> GetSessionAsync(Guid sessionId, CancellationToken ct = default);
    Task DeleteSessionAsync(Guid sessionId, string userId, CancellationToken ct = default);
}

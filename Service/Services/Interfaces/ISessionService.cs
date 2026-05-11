using Domain.Models.ImageAnalysis;
using Service.DTOs.ImageAnalysisDTOs;

namespace Service.Services.Interfaces
{
    public interface ISessionService
    {
        Task<Guid> CreateSessionAsync(AnalyzeImageResponseDto result, string? prompt, string? userId, string? imageUrl = null);
        Task<List<AnalysisData>> GetUserSessionsAsync(string userId);
        Task<AnalysisData?> GetSessionAsync(Guid sessionId);
        Task DeleteSessionAsync(Guid sessionId, string userId);
    }
}

using Domain.Models.ImageAnalysis;
using Service.DTOs.ImageAnalysisDTOs;

namespace Service.Services.Interfaces
{
    public interface ISessionService
    {
        Task<Guid> CreateSessionAsync(AnalyzeImageResponseDto result, string? prompt, string? userId);
        Task<List<AnalysisData>> GetUserSessionsAsync(string userId);
        Task<AnalysisData?> GetSessionAsync(Guid sessionId);
    }
}

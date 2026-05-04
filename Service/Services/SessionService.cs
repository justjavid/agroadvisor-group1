using Domain.Models.ImageAnalysis;
using Repository.Repositories.Interfaces;
using Service.DTOs.ImageAnalysisDTOs;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class SessionService : ISessionService
    {
        private readonly IImageAnalysisRepository _repo;

        public SessionService(IImageAnalysisRepository repo)
        {
            _repo = repo;
        }

        public async Task<Guid> CreateSessionAsync(AnalyzeImageResponseDto result, string? prompt, string? userId)
        {
            var session = new AnalysisData
            {
                SessionId = Guid.NewGuid(),
                UserId = userId,
                Prompt = prompt,
                PlantName = result.PlantName,
                DiseaseName = result.DiseaseName,
                Confidence = result.Confidence,
                AiResponseJson = System.Text.Json.JsonSerializer.Serialize(result),
                CreatedDate = DateTime.UtcNow
            };

            await _repo.AddAsync(session);
            return session.SessionId;
        }

        public async Task<List<AnalysisData>> GetUserSessionsAsync(string userId)
        {
            return await _repo.GetByUserAsync(userId);
        }

        public async Task<AnalysisData?> GetSessionAsync(Guid sessionId)
        {
            return await _repo.GetBySessionIdAsync(sessionId);
        }
    }
}

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

        public async Task<Guid> CreateSessionAsync(AnalyzeImageResponseDto result, string? prompt, string? userId, string? imageUrl = null)
        {
            var session = new AnalysisData
            {
                SessionId = Guid.NewGuid(),
                UserId = userId,
                Prompt = prompt,
                ImageUrl = imageUrl,
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

        public async Task DeleteSessionAsync(Guid sessionId, string userId)
        {
            var session = await _repo.GetBySessionIdAsync(sessionId);
            if (session == null) return;
            if (session.UserId != userId) return; // do not delete if not owner

            // delete uploaded file if present and a local uploads path
            if (!string.IsNullOrWhiteSpace(session.ImageUrl) && session.ImageUrl.StartsWith("/uploads/"))
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", session.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(filePath))
                {
                    try { System.IO.File.Delete(filePath); } catch { /* ignore file delete errors */ }
                }
            }

            await _repo.DeleteAsync(sessionId);
        }
    }
}

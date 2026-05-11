using System.Text.Json;
using Domain.Models.ImageAnalysis;
using Microsoft.Extensions.Hosting;
using Repository.Repositories.Interfaces;
using Service.DTOs.ImageAnalysisDTOs;
using Service.Services.ImageAnalysis.Interfaces;

namespace Service.Services.ImageAnalysis;

public class AnalysisSessionService : IAnalysisSessionService
{
    private readonly IImageAnalysisRepository _repo;
    private readonly IHostEnvironment _env;

    public AnalysisSessionService(IImageAnalysisRepository repo, IHostEnvironment env)
    {
        _repo = repo;
        _env = env;
    }

    public async Task<Guid> CreateSessionAsync(
        AnalyzeImageResponseDto result,
        string? prompt,
        string? userId,
        string? imageUrl,
        CancellationToken ct = default)
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
            AiResponseJson = JsonSerializer.Serialize(result),
            CreatedDate = DateTime.UtcNow
        };

        await _repo.AddAsync(session, ct);
        return session.SessionId;
    }

    public Task<List<AnalysisData>> GetUserSessionsAsync(string userId, CancellationToken ct = default) =>
        _repo.GetByUserAsync(userId, ct);

    public Task<AnalysisData?> GetSessionAsync(Guid sessionId, CancellationToken ct = default) =>
        _repo.GetBySessionIdAsync(sessionId, ct);

    public async Task DeleteSessionAsync(Guid sessionId, string userId, CancellationToken ct = default)
    {
        var session = await _repo.GetBySessionIdAsync(sessionId, ct);
        if (session is null) throw new KeyNotFoundException("Analysis session not found.");
        if (session.UserId != userId) throw new UnauthorizedAccessException("You do not own this session.");

        // Delete the uploaded file (if any) before removing the DB row.
        if (!string.IsNullOrWhiteSpace(session.ImageUrl) && session.ImageUrl.StartsWith("/uploads/"))
        {
            var webRoot = _env.ContentRootPath;
            var filePath = Path.Combine(webRoot, "wwwroot", session.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(filePath))
            {
                try { File.Delete(filePath); } catch { /* best-effort */ }
            }
        }

        await _repo.DeleteAsync(sessionId, ct);
    }
}

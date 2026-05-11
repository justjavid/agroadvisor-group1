using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Service.DTOs.ImageAnalysisDTOs;
using Service.Services.ImageAnalysis.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AgroAdvisor.Controllers;

[Authorize]
[Route("api/[controller]/[action]")]
[ApiController]
public class IAController : ControllerBase
{
    private readonly IImageAnalysisService _imageService;
    private readonly IImageSearchService _imageSearchService;
    private readonly IAnalysisSessionService _sessionService;
    private readonly IWebHostEnvironment _env;

    public IAController(
        IImageAnalysisService imageService,
        IImageSearchService imageSearchService,
        IAnalysisSessionService sessionService,
        IWebHostEnvironment env)
    {
        _imageService = imageService;
        _imageSearchService = imageSearchService;
        _sessionService = sessionService;
        _env = env;
    }

    [HttpPost]
    public async Task<IActionResult> Analyze([FromForm] AnalyzeFormRequestDto request, CancellationToken ct)
    {
        if (request?.File == null || request.File.Length == 0)
            return BadRequest("File is required");

        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        using var ms = new MemoryStream();
        await request.File.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        // Persist the uploaded image under wwwroot/uploads so we can serve it back
        // when the user requests their past analyses.
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var uploadsDir = Path.Combine(webRoot, "uploads");
        Directory.CreateDirectory(uploadsDir);
        var extension = Path.GetExtension(request.File.FileName);
        if (string.IsNullOrWhiteSpace(extension)) extension = ".jpg";
        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsDir, fileName);
        await System.IO.File.WriteAllBytesAsync(filePath, bytes, ct);
        var imageUrl = $"/uploads/{fileName}";

        var dto = new AnalyzeImageRequestDto
        {
            ImageBase64 = Convert.ToBase64String(bytes),
            Prompt = request.Prompt
        };

        var result = await _imageService.AnalyzeImageAsync(dto);

        if (!string.IsNullOrWhiteSpace(result.DiseaseName))
        {
            var query = $"{result.DiseaseName} plant leaf close up infected leaves symptoms agriculture";
            var images = await _imageSearchService.SearchImagesAsync(query);
            result.ImageUrls = images;
        }

        var sessionId = await _sessionService.CreateSessionAsync(result, request.Prompt, userId, imageUrl, ct);

        // Return the analysis result plus the new sessionId+imageUrl so the
        // frontend can navigate to / fetch the persisted session if needed.
        return Ok(new
        {
            sessionId,
            imageUrl,
            result.PlantName,
            result.DiseaseName,
            result.Information,
            result.Confidence,
            result.ImageUrls
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetMySessions(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var sessions = await _sessionService.GetUserSessionsAsync(userId, ct);
        return Ok(sessions);
    }

    [HttpGet("{sessionId:guid}")]
    public async Task<IActionResult> GetSession(Guid sessionId, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var session = await _sessionService.GetSessionAsync(sessionId, ct);
        if (session is null) return NotFound(new { error = "Analysis session not found." });
        if (session.UserId != userId) return NotFound(new { error = "Analysis session not found." });

        return Ok(session);
    }

    [HttpDelete("{sessionId:guid}")]
    public async Task<IActionResult> DeleteSession(Guid sessionId, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        await _sessionService.DeleteSessionAsync(sessionId, userId, ct);
        return NoContent();
    }

    private string? GetUserId() =>
        User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
}

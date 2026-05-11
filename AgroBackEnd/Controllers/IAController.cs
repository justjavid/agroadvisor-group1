using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.DTOs.ImageAnalysisDTOs;
using Service.Services.Interfaces;
using System.IO;

namespace AgroBackEnd.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class IAController : ControllerBase
    {
        private readonly IImageAnalysisService _imageService;
        private readonly IImageSearchService _imageSearchService;
        private readonly ISessionService _sessionService;

        public IAController(IImageAnalysisService imageService, 
           IImageSearchService imageSearchService,
           ISessionService sessionService)
        {
            _imageService = imageService;
            _imageSearchService = imageSearchService;
            _sessionService = sessionService;
        }

        [HttpPost]
        public async Task<IActionResult> Analyze([FromForm] AnalyzeFormRequestDto request)
        {
            if (request?.File == null || request.File.Length == 0)
                return BadRequest("File is required");

            using var ms = new MemoryStream();
            await request.File.CopyToAsync(ms);
            // Save uploaded file to wwwroot/uploads and set ImageUrl
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(request.File.FileName)}";
            var filePath = Path.Combine(uploadsDir, fileName);
            await System.IO.File.WriteAllBytesAsync(filePath, ms.ToArray());
            var imageUrl = $"/uploads/{fileName}";
            var dto = new AnalyzeImageRequestDto
            {
                ImageBase64 = Convert.ToBase64String(ms.ToArray()),
                Prompt = request.Prompt
            };


            var result = await _imageService.AnalyzeImageAsync(dto);

            if (!string.IsNullOrWhiteSpace(result.DiseaseName))
            {
                var query = $"{result.DiseaseName} plant leaf close up infected leaves symptoms agriculture";

                var images = await _imageSearchService.SearchImagesAsync(query);

                result.ImageUrls = images;
            }

            // Save session using session service
            var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var sessionId = await _sessionService.CreateSessionAsync(result, dto.Prompt, userId, imageUrl);

            return Ok(new { sessionId, result });

        }

        [HttpGet]
        public async Task<IActionResult> GetMySessions()
        {
            var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var sessions = await _sessionService.GetUserSessionsAsync(userId);
            return Ok(sessions);
        }

        [HttpGet("{sessionId}")]
        public async Task<IActionResult> GetSession(Guid sessionId)
        {
            var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var session = await _sessionService.GetSessionAsync(sessionId);
            if (session == null || session.UserId != userId)
                return NotFound();

            return Ok(session);
        }

        [HttpDelete("{sessionId}")]
        public async Task<IActionResult> DeleteSession(Guid sessionId)
        {
            var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var session = await _sessionService.GetSessionAsync(sessionId);
            if (session == null || session.UserId != userId)
                return NotFound();

            await _sessionService.DeleteSessionAsync(sessionId, userId);
            return NoContent();
        }







    }
}

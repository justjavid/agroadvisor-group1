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
        private readonly Service.Services.Interfaces.ISessionService _sessionService;

        public IAController(IImageAnalysisService imageService, IImageSearchService imageSearchService,
            Service.Services.Interfaces.ISessionService sessionService)
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
            var sessionId = await _sessionService.CreateSessionAsync(result, dto.Prompt, userId);

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







    }
}

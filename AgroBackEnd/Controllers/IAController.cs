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

        public IAController(IImageAnalysisService imageService)
        {
            _imageService = imageService;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]

        public async Task<IActionResult> Analyze([FromForm] AnalyzeFormRequestDto request)
        {
            var file = request?.File;
            var prompt = request?.Prompt;

            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var fileBytes = ms.ToArray();
            var base64 = Convert.ToBase64String(fileBytes);

            var dto = new AnalyzeImageRequestDto
            {
                ImageBase64 = base64,
                Prompt = prompt
            };

            AnalyzeImageResponseDto result;
            try
            {
                result = await _imageService.AnalyzeImageAsync(dto);

                // Only return AI-provided image. If AI didn't return an image, leave ImageBase64 null.
                if (result == null || string.IsNullOrWhiteSpace(result.ImageBase64))
                {
                    if (result != null)
                        result.ImageBase64 = null;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }

            return Ok(result);
        }







    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.DTOs.ImageAnalysisDTOs;
using Service.Services.ImageAnalysis.Interfaces;
using System.IO;

namespace AgroAdvisor.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class IAController : ControllerBase
    {
        private readonly IImageAnalysisService _imageService;
        private readonly IImageSearchService _imageSearchService;

        public IAController(IImageAnalysisService imageService, IImageSearchService imageSearchService)
        {
            _imageService = imageService;
            _imageSearchService = imageSearchService;
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

            // 🔥 PEXELS INTEGRATION
            if (!string.IsNullOrWhiteSpace(result.DiseaseName))
            {
                var query = $"{result.PlantName} {result.DiseaseName}";
                var images = await _imageSearchService.SearchPhotosAsync(query);

                result.ImageUrls = images;
            }

            return Ok(result);

        }







    }
}
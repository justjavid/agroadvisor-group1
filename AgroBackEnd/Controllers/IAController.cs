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

            if (!string.IsNullOrWhiteSpace(result.DiseaseName))
            {
                var query = $"{result.DiseaseName} plant leaf close up infected leaves symptoms agriculture";

                var images = await _imageSearchService.SearchImagesAsync(query);

                result.ImageUrls = images;
            }

            return Ok(result);

        }







    }
}

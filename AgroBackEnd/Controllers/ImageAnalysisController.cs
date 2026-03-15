using Microsoft.AspNetCore.Mvc;
using Service.DTOs.ImageAnalysisDTOs;
using Service.Services.Interfaces;

namespace AgroBackEnd.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImageAnalysisController : ControllerBase
{
    private readonly IImageAnalysisService _imageAnalysisService;

    public ImageAnalysisController(IImageAnalysisService imageAnalysisService)
    {
        _imageAnalysisService = imageAnalysisService;
    }

    /// <summary>
    /// Bitki xəstəliyini şəkildən diaqnostika edir.
    /// Form-data: image (file), plantType (optional), userId (optional)
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(10_000_000)] // 10 MB
    public async Task<ActionResult<ImageAnalysisResultDto>> Analyze(
        IFormFile image,
        [FromForm] string? plantType,
        [FromForm] string? userId,
        CancellationToken cancellationToken)
    {
        if (image is null || image.Length == 0)
        {
            return BadRequest(new { error = "Şəkil faylı göndərilməyib." });
        }

        await using var ms = new MemoryStream();
        await image.CopyToAsync(ms, cancellationToken);

        var request = new ImageAnalysisRequestDto
        {
            ImageBytes = ms.ToArray(),
            ContentType = image.ContentType,
            PlantType = plantType,
            UserId = userId
        };

        var result = await _imageAnalysisService.AnalyzeAsync(request, cancellationToken);
        return Ok(result);
    }
}


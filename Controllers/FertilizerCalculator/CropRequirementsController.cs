using Microsoft.AspNetCore.Mvc;
using Service.DTOs.FertilizerCalculatorDTOs.Requests;
using Service.DTOs.FertilizerCalculatorDTOs.Responses;
using Service.Services.FertilizerCalculator;
using Service.Services.FertilizerCalculator.Interfaces;

namespace AgroAdvisor.Controllers.FertilizerCalculator;

[ApiController]
[Route("api/[controller]")]
public class CropRequirementsController : ControllerBase
{
    private readonly ICropRequirementsService _cropRequirementsService;

    public CropRequirementsController(ICropRequirementsService cropRequirementsService)
    {
        _cropRequirementsService = cropRequirementsService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AddCropRequirementsResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AddCropRequirementsResponse>> Post([FromBody] AddCropRequirementsRequest request, CancellationToken cancellationToken)
    {
        var created = await _cropRequirementsService.AddAsync(request, cancellationToken);
        return Created($"/api/croprequirements/{created.Id}", created);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<AddCropRequirementsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AddCropRequirementsResponse>>> Get(CancellationToken cancellationToken)
    {
        var items = await _cropRequirementsService.GetAllAsync(cancellationToken);
        return Ok(items);
    }
}
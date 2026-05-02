using Microsoft.AspNetCore.Mvc;
using Service.DTOs.FertilizerCalculatorDTOs.Requests;
using Service.DTOs.FertilizerCalculatorDTOs.Responses;
using Service.Services.FertilizerCalculator;
using Service.Services.FertilizerCalculator.Interfaces;

namespace AgroAdvisor.Controllers.FertilizerCalculator;

[ApiController]
[Route("api/[controller]")]
public class SoilMultipliersController : ControllerBase
{
    private readonly ISoilMultiplierService _soilMultiplierService;

    public SoilMultipliersController(ISoilMultiplierService soilMultiplierService)
    {
        _soilMultiplierService = soilMultiplierService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AddSoilMultiplierResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AddSoilMultiplierResponse>> Post([FromBody] AddSoilMultiplierRequest request, CancellationToken cancellationToken)
    {
        var created = await _soilMultiplierService.AddAsync(request, cancellationToken);
        return Created($"/api/soilmultipliers/{created.Id}", created);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<AddSoilMultiplierResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AddSoilMultiplierResponse>>> Get(CancellationToken cancellationToken)
    {
        var items = await _soilMultiplierService.GetAllAsync(cancellationToken);
        return Ok(items);
    }
}
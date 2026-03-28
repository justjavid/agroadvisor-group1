using Microsoft.AspNetCore.Mvc;
using Service.DTOs.FertilizerCalculatorDTOs.Requests;
using Service.DTOs.FertilizerCalculatorDTOs.Responses;
using Service.Services.Interfaces;

namespace AgroBackEnd.Controllers;

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
}

using Microsoft.AspNetCore.Mvc;
using Service.DTOs.FertilizerCalculatorDTOs.Requests;
using Service.DTOs.FertilizerCalculatorDTOs.Responses;
using Service.Services.FertilizerCalculator;
using Service.Services.FertilizerCalculator.Interfaces;

namespace AgroAdvisor.Controllers.FertilizerCalculator;

[ApiController]
[Route("api/[controller]")]
public class FertilizersController : ControllerBase
{
    private readonly IFertilizerService _fertilizerService;

    public FertilizersController(IFertilizerService fertilizerService)
    {
        _fertilizerService = fertilizerService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AddFertilizerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AddFertilizerResponse>> Post([FromBody] AddFertilizerRequest request, CancellationToken cancellationToken)
    {
        var created = await _fertilizerService.AddAsync(request, cancellationToken);
        return Created($"/api/fertilizers/{created.Id}", created);
    }
}
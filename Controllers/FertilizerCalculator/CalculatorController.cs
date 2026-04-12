using Microsoft.AspNetCore.Mvc;
using Service.DTOs.FertilizerCalculatorDTOs.Requests;
using Service.DTOs.FertilizerCalculatorDTOs.Responses;
using Service.Services.FertilizerCalculator;
using Service.Services.FertilizerCalculator.Interfaces;

namespace AgroAdvisor.Controllers.FertilizerCalculator;

[ApiController]
[Route("api/[controller]")]
public class CalculatorController : ControllerBase
{
    private readonly ICalculatorService _calculatorService;

    public CalculatorController(ICalculatorService calculatorService)
    {
        _calculatorService = calculatorService;
    }

    [HttpPost("calculate")]
    [ProducesResponseType(typeof(CalculatorResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CalculatorResultResponse>> Calculate(
        [FromBody] CalculatorInputRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _calculatorService.CalculateAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
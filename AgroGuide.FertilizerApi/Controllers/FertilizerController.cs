using AgroGuide.FertilizerApi.DTOs;
using AgroGuide.FertilizerApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgroGuide.FertilizerApi.Controllers;

[ApiController]
[Route("api/fertilizer")]
public class FertilizerController : ControllerBase
{
    private readonly FertilizerAgent _agent;

    public FertilizerController(FertilizerAgent agent)
    {
        _agent = agent;
    }

    [HttpPost]
    public async Task<ActionResult<FertilizerResult>> Post(
        [FromBody] FarmerInput input,
        CancellationToken cancellationToken)
    {
        var result = await _agent.GetRecommendationAsync(input, cancellationToken);
        if (!string.IsNullOrEmpty(result.Error))
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}

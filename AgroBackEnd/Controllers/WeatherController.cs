using Domain;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Service.Services.Interfaces;

namespace AgroBackEnd.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;

    public WeatherController(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    [HttpGet]
    public async Task<IActionResult> GetWeather(
        [FromQuery] double latitude,
        [FromQuery] double longitude)
    {
        var request = new WeatherRequest
        {
            Latitude = latitude,
            Longitude = longitude
        };

        var result = await _weatherService.GetWeatherAsync(request);

        if (result == null)
            return StatusCode(503, "Weather data could not be retrieved.");

        return Ok(result);
    }
}
using Domain;
using Domain.Models;

namespace Service.Services.Interfaces;

public interface IWeatherService
{
    Task<WeatherResponse?> GetWeatherAsync(WeatherRequest request);
}
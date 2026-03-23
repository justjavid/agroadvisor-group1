using Domain;
using Domain.Models;
using Service.Services.Interfaces;
using System.Text.Json;

namespace Service.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;

    public WeatherService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<WeatherResponse?> GetWeatherAsync(WeatherRequest request)
    {
        // Build the Open-Meteo URL
        var url = $"https://api.open-meteo.com/v1/forecast" +
                  $"?latitude={request.Latitude}" +
                  $"&longitude={request.Longitude}" +
                  $"&current_weather=true";

        try
        {
            // Call the API
            var response = await _httpClient.GetAsync(url);

            // If the API returned an error, return null
            if (!response.IsSuccessStatusCode)
                return null;

            // Read the raw JSON string
            var json = await response.Content.ReadAsStringAsync();

            // Parse the JSON
            var doc = JsonDocument.Parse(json);
            var current = doc.RootElement.GetProperty("current_weather");

            // Extract the fields we care about
            double temperature = current.GetProperty("temperature").GetDouble();
            double windspeed = current.GetProperty("windspeed").GetDouble();
            int weatherCode = current.GetProperty("weathercode").GetInt32();

            // Return a clean response model
            return new WeatherResponse
            {
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                CurrentTemperature = temperature,
                WindSpeed = windspeed,
                Summary = GetSummary(weatherCode)
            };
        }
        catch
        {
            // If anything goes wrong (network, parse error), return null
            return null;
        }
    }

    // Translate Open-Meteo weather codes into human-readable text
    private static string GetSummary(int code)
    {
        return code switch
        {
            0 => "Clear sky",
            1 or 2 or 3 => "Partly cloudy",
            45 or 48 => "Foggy",
            51 or 53 or 55 => "Drizzle",
            61 or 63 or 65 => "Rainy",
            71 or 73 or 75 => "Snowy",
            80 or 81 or 82 => "Rain showers",
            95 => "Thunderstorm",
            _ => "Unknown"
        };
    }
}
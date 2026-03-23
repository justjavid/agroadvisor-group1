namespace Domain.Models;

public class WeatherResponse
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double CurrentTemperature { get; set; }
    public double WindSpeed { get; set; }
    public string Summary { get; set; } = string.Empty;
}
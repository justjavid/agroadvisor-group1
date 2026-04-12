namespace Service.DTOs.FertilizerCalculatorDTOs.Responses;

public class AddSoilMultiplierResponse
{
    public int Id { get; set; }
    public string SoilType { get; set; } = string.Empty;
    public double NMultiplier { get; set; }
    public double PMultiplier { get; set; }
    public double KMultiplier { get; set; }
}
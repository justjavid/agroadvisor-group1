namespace Domain.Models.FertilizerCalculator;

public class SoilMultiplier
{
    public int Id { get; set; }
    public string SoilType { get; set; }
    public double NMultiplier { get; set; }
    public double PMultiplier { get; set; }
    public double KMultiplier { get; set; }
}
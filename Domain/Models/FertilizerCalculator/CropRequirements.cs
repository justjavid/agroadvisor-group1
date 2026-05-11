namespace Domain.Models.FertilizerCalculator;

public class CropRequirements
{
    public int Id { get; set; }
    public string CropType { get; set; }
    public string GrowthStage { get; set; }
    public double N { get; set; }
    public double P { get; set; }
    public double K { get; set; }
}
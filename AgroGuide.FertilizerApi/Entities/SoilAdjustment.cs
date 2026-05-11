namespace AgroGuide.FertilizerApi.Entities;

public class SoilAdjustment
{
    public int Id { get; set; }
    public string Nutrient { get; set; } = string.Empty;
    public string SoilLevel { get; set; } = string.Empty;
    public double ReductionPercent { get; set; }
}

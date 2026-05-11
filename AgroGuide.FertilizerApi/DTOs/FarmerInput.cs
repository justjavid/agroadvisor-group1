namespace AgroGuide.FertilizerApi.DTOs;

public class FarmerInput
{
    public string Crop { get; set; } = string.Empty;
    public double AreaHectares { get; set; }
    public double TargetYield { get; set; }
    public string SoilN { get; set; } = string.Empty;
    public string SoilP { get; set; } = string.Empty;
    public string SoilK { get; set; } = string.Empty;
    public double SoilPh { get; set; }
}

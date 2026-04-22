namespace Service.DTOs.FertilizerCalculatorDTOs.Responses;

public class CalculatorResultResponse
{
    public string CropType { get; set; } = string.Empty;
    public string GrowthStage { get; set; } = string.Empty;
    public string SoilType { get; set; } = string.Empty;
    public double FieldSizeHectares { get; set; }

    public double BaseNPerHectare { get; set; }
    public double BasePPerHectare { get; set; }
    public double BaseKPerHectare { get; set; }

    public double AppliedNMultiplier { get; set; }
    public double AppliedPMultiplier { get; set; }
    public double AppliedKMultiplier { get; set; }

    public double TotalNRequired { get; set; }
    public double TotalPRequired { get; set; }
    public double TotalKRequired { get; set; }

    public string? AiSummary { get; set; }
    public List<string> AiSuggestions { get; set; } = [];
}
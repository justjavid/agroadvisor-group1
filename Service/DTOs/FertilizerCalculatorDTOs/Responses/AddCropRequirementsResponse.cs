namespace Service.DTOs.FertilizerCalculatorDTOs.Responses;

public class AddCropRequirementsResponse
{
    public int Id { get; set; }
    public string CropType { get; set; } = string.Empty;
    public string GrowthStage { get; set; } = string.Empty;
    public double N { get; set; }
    public double P { get; set; }
    public double K { get; set; }
}
using System.ComponentModel.DataAnnotations;

namespace Service.DTOs.FertilizerCalculatorDTOs.Requests;

public class AddCropRequirementsRequest
{
    [Required]
    [MaxLength(500)]
    public string CropType { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string GrowthStage { get; set; } = string.Empty;

    public double N { get; set; }
    public double P { get; set; }
    public double K { get; set; }
}

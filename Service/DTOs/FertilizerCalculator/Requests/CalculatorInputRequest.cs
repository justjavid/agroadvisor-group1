using System.ComponentModel.DataAnnotations;

namespace Service.DTOs.FertilizerCalculatorDTOs.Requests;

public class CalculatorInputRequest
{
    [Required]
    [MaxLength(500)]
    public string CropType { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string GrowthStage { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string SoilType { get; set; } = string.Empty;

    [Range(0.000001, double.MaxValue)]
    public double FieldSizeHectares { get; set; }
}

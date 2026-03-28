using System.ComponentModel.DataAnnotations;

namespace Service.DTOs.FertilizerCalculatorDTOs.Requests;

public class AddSoilMultiplierRequest
{
    [Required]
    [MaxLength(500)]
    public string SoilType { get; set; } = string.Empty;

    public double NMultiplier { get; set; }
    public double PMultiplier { get; set; }
    public double KMultiplier { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace Service.DTOs.FertilizerCalculatorDTOs.Requests;

public class AddFertilizerRequest
{
    [Required]
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 100)]
    public double NPct { get; set; }

    [Range(0, 100)]
    public double PPct { get; set; }

    [Range(0, 100)]
    public double KPct { get; set; }

    public double? CostPerKg { get; set; }
}
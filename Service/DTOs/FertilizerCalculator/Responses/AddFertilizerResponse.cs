namespace Service.DTOs.FertilizerCalculatorDTOs.Responses;

public class AddFertilizerResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double NPct { get; set; }
    public double PPct { get; set; }
    public double KPct { get; set; }
    public double? CostPerKg { get; set; }
}

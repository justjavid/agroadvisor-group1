namespace Domain.Models.FertilizerCalculator;

public class Fertilizer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public double NPct { get; set; }
    public double PPct { get; set; }
    public double KPct { get; set; }
    public double? CostPerKg { get; set; }
}
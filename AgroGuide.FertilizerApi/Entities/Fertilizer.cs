namespace AgroGuide.FertilizerApi.Entities;

public class Fertilizer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double NPercent { get; set; }
    public double PPercent { get; set; }
    public double KPercent { get; set; }
}

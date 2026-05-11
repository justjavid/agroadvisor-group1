namespace AgroGuide.FertilizerApi.Entities;

public class ApplicationSchedule
{
    public int Id { get; set; }
    public int CropId { get; set; }
    public string Stage { get; set; } = string.Empty;
    public string Timing { get; set; } = string.Empty;
    public string NutrientsToApply { get; set; } = string.Empty;

    public Crop Crop { get; set; } = null!;
}

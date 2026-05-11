namespace AgroGuide.FertilizerApi.Entities;

public class Crop
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double NPerTon { get; set; }
    public double PPerTon { get; set; }
    public double KPerTon { get; set; }

    public ICollection<ApplicationSchedule> ApplicationSchedules { get; set; } =
        new List<ApplicationSchedule>();
}

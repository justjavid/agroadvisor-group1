using System.Text.Json.Serialization;

namespace AgroGuide.FertilizerApi.DTOs;

public class FertilizerResult
{
    [JsonPropertyName("nutrient_requirements")]
    public NutrientRequirements? NutrientRequirements { get; set; }

    [JsonPropertyName("fertilizer_quantities")]
    public FertilizerQuantities? FertilizerQuantities { get; set; }

    [JsonPropertyName("application_schedule")]
    public List<ApplicationScheduleItem>? ApplicationSchedule { get; set; }

    [JsonPropertyName("warnings")]
    public List<string>? Warnings { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class NutrientRequirements
{
    [JsonPropertyName("N_kg")]
    public double N_kg { get; set; }

    [JsonPropertyName("P_kg")]
    public double P_kg { get; set; }

    [JsonPropertyName("K_kg")]
    public double K_kg { get; set; }
}

public class FertilizerQuantities
{
    [JsonPropertyName("Urea_kg")]
    public double Urea_kg { get; set; }

    [JsonPropertyName("DAP_kg")]
    public double DAP_kg { get; set; }

    [JsonPropertyName("MOP_kg")]
    public double MOP_kg { get; set; }
}

public class ApplicationScheduleItem
{
    [JsonPropertyName("stage")]
    public string Stage { get; set; } = string.Empty;

    [JsonPropertyName("timing")]
    public string Timing { get; set; } = string.Empty;

    [JsonPropertyName("products")]
    public string Products { get; set; } = string.Empty;
}

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AgroGuide.FertilizerApi.Data;
using AgroGuide.FertilizerApi.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AgroGuide.FertilizerApi.Services;

public class FertilizerAgent
{
    private readonly AppDbContext _db;
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions TableJsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly JsonSerializerOptions LlmJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions OllamaRequestOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public FertilizerAgent(AppDbContext db, HttpClient http)
    {
        _db = db;
        _http = http;
    }

    public async Task<string> BuildPrompt(FarmerInput input, CancellationToken cancellationToken = default)
    {
        var crops = await _db.Crops.AsNoTracking().OrderBy(c => c.Id).ToListAsync(cancellationToken);
        var soilAdjustments = await _db.SoilAdjustments.AsNoTracking().OrderBy(s => s.Id).ToListAsync(cancellationToken);
        var fertilizers = await _db.Fertilizers.AsNoTracking().OrderBy(f => f.Id).ToListAsync(cancellationToken);
        var schedule = await _db.ApplicationSchedules.AsNoTracking()
            .OrderBy(s => s.CropId).ThenBy(s => s.Id)
            .Select(s => new
            {
                s.Id,
                s.CropId,
                s.Stage,
                s.Timing,
                s.NutrientsToApply
            })
            .ToListAsync(cancellationToken);

        var cropsJson = JsonSerializer.Serialize(crops, TableJsonOptions);
        var soilJson = JsonSerializer.Serialize(soilAdjustments, TableJsonOptions);
        var fertilizersJson = JsonSerializer.Serialize(fertilizers, TableJsonOptions);
        var scheduleJson = JsonSerializer.Serialize(schedule, TableJsonOptions);

        return PromptTemplate
            .Replace("{crops}", cropsJson)
            .Replace("{soilAdjustments}", soilJson)
            .Replace("{fertilizers}", fertilizersJson)
            .Replace("{schedule}", scheduleJson)
            .Replace("{input.Crop}", input.Crop)
            .Replace("{input.AreaHectares}", input.AreaHectares.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Replace("{input.TargetYield}", input.TargetYield.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Replace("{input.SoilN}", input.SoilN)
            .Replace("{input.SoilP}", input.SoilP)
            .Replace("{input.SoilK}", input.SoilK)
            .Replace("{input.SoilPh}", input.SoilPh.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public async Task<FertilizerResult> GetRecommendationAsync(
        FarmerInput input,
        CancellationToken cancellationToken = default)
    {
        var prompt = await BuildPrompt(input, cancellationToken);

        var ollamaRequest = new OllamaGenerateRequest("llama3", prompt, Stream: false);
        using var content = new StringContent(
            JsonSerializer.Serialize(ollamaRequest, OllamaRequestOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await _http.PostAsync("api/generate", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            return new FertilizerResult
            {
                Error = $"Ollama request failed ({(int)response.StatusCode}): {err}"
            };
        }

        var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(LlmJsonOptions, cancellationToken);
        if (ollamaResponse?.Response is null || string.IsNullOrWhiteSpace(ollamaResponse.Response))
        {
            return new FertilizerResult { Error = "Empty response from model." };
        }

        var jsonPayload = ExtractJsonBlock(ollamaResponse.Response);
        try
        {
            var result = JsonSerializer.Deserialize<FertilizerResult>(jsonPayload, LlmJsonOptions);
            return result ?? new FertilizerResult { Error = "Failed to parse model output." };
        }
        catch (JsonException)
        {
            return new FertilizerResult { Error = "Model returned invalid JSON." };
        }
    }

    private static string ExtractJsonBlock(string text)
    {
        var t = text.Trim();
        if (t.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNl = t.IndexOf('\n');
            if (firstNl >= 0)
            {
                t = t[(firstNl + 1)..].Trim();
            }

            var fenceEnd = t.LastIndexOf("```", StringComparison.Ordinal);
            if (fenceEnd > 0)
            {
                t = t[..fenceEnd].Trim();
            }
        }

        var start = t.IndexOf('{');
        var end = t.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return t[start..(end + 1)];
        }

        return t;
    }

    private sealed record OllamaGenerateRequest(string Model, string Prompt, bool Stream);

    private sealed class OllamaGenerateResponse
    {
        public string? Response { get; set; }
    }

    private const string PromptTemplate =
"""
You are an agricultural fertilizer recommendation agent.

## YOUR DATA

### Crops Table
{crops}

### Soil Adjustments Table
{soilAdjustments}

### Fertilizers Table
{fertilizers}

### Application Schedule Table
{schedule}

## FARMER INPUTS
- Crop: {input.Crop}
- Field Area: {input.AreaHectares} hectares
- Target Yield: {input.TargetYield} tons/hectare
- Soil N Level: {input.SoilN}
- Soil P Level: {input.SoilP}
- Soil K Level: {input.SoilK}
- Soil pH: {input.SoilPh}

## CALCULATION STEPS — FOLLOW EXACTLY

### Step 1: Calculate total nutrient demand
- Find the crop in the crops table
- Multiply N_per_ton, P_per_ton, K_per_ton by target_yield
- Multiply result by field area
- This gives total N, P, K needed in kg

### Step 2: Apply soil adjustments
- For each nutrient (N, P, K), look up the soil level in soil_adjustments table
- Reduce the nutrient amount by the reduction_percent
- This gives adjusted N, P, K in kg

### Step 3: Apply pH warning
- If pH < 6.0 → add warning: "Soil is acidic. Consider liming before fertilization."
- If pH > 7.5 → add warning: "Soil is alkaline. Phosphorus availability may be reduced."
- Otherwise → no pH warning

### Step 4: Convert nutrients to fertilizer products
- For N: use Urea. Formula → Urea_kg = adjusted_N / 0.46
- For P: use DAP. Formula → DAP_kg = adjusted_P / 0.46
- DAP also contributes N → subtract (DAP_kg × 0.18) from Urea requirement
- For K: use MOP. Formula → MOP_kg = adjusted_K / 0.60
- Round all values to nearest whole number

### Step 5: Build application schedule
- Look up the crop in application_schedule table
- Assign fertilizer quantities to each stage

## OUTPUT FORMAT
Respond only in this JSON format, nothing else:

{
  "nutrient_requirements": { "N_kg": 0, "P_kg": 0, "K_kg": 0 },
  "fertilizer_quantities": { "Urea_kg": 0, "DAP_kg": 0, "MOP_kg": 0 },
  "application_schedule": [
    { "stage": "", "timing": "", "products": "" }
  ],
  "warnings": []
}

## RULES
- Never guess values not in the tables
- If crop not found → { "error": "Crop not found" }
- If any soil input missing → { "error": "Missing soil input: <field>" }
- No text outside the JSON
---
""";
}

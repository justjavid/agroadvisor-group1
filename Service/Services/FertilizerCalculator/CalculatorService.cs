using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Service.DTOs.FertilizerCalculatorDTOs.Requests;
using Service.DTOs.FertilizerCalculatorDTOs.Responses;
using Service.Services.Interfaces;

namespace Service.Services;

public class CalculatorService : ICalculatorService
{
    private readonly FertilizerCalculatorDbContext _db;
    private readonly IFertilizerAiInsightService _fertilizerAiInsightService;

    public CalculatorService(FertilizerCalculatorDbContext db, IFertilizerAiInsightService fertilizerAiInsightService)
    {
        _db = db;
        _fertilizerAiInsightService = fertilizerAiInsightService;
    }

    public async Task<CalculatorResultResponse> CalculateAsync(CalculatorInputRequest request, CancellationToken cancellationToken = default)
    {
        var cropRequirements = await _db.CropRequirements
            .FirstOrDefaultAsync(
                x => x.CropType.ToLower() == request.CropType.ToLower()
                    && x.GrowthStage.ToLower() == request.GrowthStage.ToLower(),
                cancellationToken);

        if (cropRequirements is null)
        {
            throw new KeyNotFoundException("Crop requirements not found for the provided crop type and growth stage.");
        }

        var soilMultiplier = await _db.SoilMultiplier
            .FirstOrDefaultAsync(x => x.SoilType.ToLower() == request.SoilType.ToLower(), cancellationToken);

        if (soilMultiplier is null)
        {
            throw new KeyNotFoundException("Soil multiplier not found for the provided soil type.");
        }

        var totalNRequired = cropRequirements.N * soilMultiplier.NMultiplier * request.FieldSizeHectares;
        var totalPRequired = cropRequirements.P * soilMultiplier.PMultiplier * request.FieldSizeHectares;
        var totalKRequired = cropRequirements.K * soilMultiplier.KMultiplier * request.FieldSizeHectares;

        var result = new CalculatorResultResponse
        {
            CropType = cropRequirements.CropType,
            GrowthStage = cropRequirements.GrowthStage,
            SoilType = soilMultiplier.SoilType,
            FieldSizeHectares = request.FieldSizeHectares,
            BaseNPerHectare = cropRequirements.N,
            BasePPerHectare = cropRequirements.P,
            BaseKPerHectare = cropRequirements.K,
            AppliedNMultiplier = soilMultiplier.NMultiplier,
            AppliedPMultiplier = soilMultiplier.PMultiplier,
            AppliedKMultiplier = soilMultiplier.KMultiplier,
            TotalNRequired = totalNRequired,
            TotalPRequired = totalPRequired,
            TotalKRequired = totalKRequired
        };

        try
        {
            var aiInsight = await _fertilizerAiInsightService.GenerateInsightsAsync(result, cancellationToken);
            result.AiSummary = aiInsight.Summary;
            result.AiSuggestions = aiInsight.Suggestions;
        }
        catch (Exception ex)
        {
            result.AiSummary = "AI xidməti hal-hazırda əlçatmazdır.";
            result.AiSuggestions = [$"Xəta: {ex.GetType().Name}: {ex.Message}"];
        }

        return result;
    }
}

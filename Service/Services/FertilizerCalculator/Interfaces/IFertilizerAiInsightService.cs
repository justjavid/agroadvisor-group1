using Service.DTOs.FertilizerCalculatorDTOs.Responses;

namespace Service.Services.FertilizerCalculator.Interfaces;

public interface IFertilizerAiInsightService
{
    Task<(string Summary, List<string> Suggestions)> GenerateInsightsAsync(
        CalculatorResultResponse result,
        CancellationToken cancellationToken = default);
}

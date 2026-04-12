using Service.DTOs.FertilizerCalculatorDTOs.Requests;
using Service.DTOs.FertilizerCalculatorDTOs.Responses;

namespace Service.Services.FertilizerCalculator.Interfaces;

public interface ICalculatorService
{
    Task<CalculatorResultResponse> CalculateAsync(CalculatorInputRequest request, CancellationToken cancellationToken = default);
}
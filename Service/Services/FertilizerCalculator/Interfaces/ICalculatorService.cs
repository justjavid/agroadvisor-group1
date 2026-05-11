using Service.DTOs.FertilizerCalculatorDTOs.Requests;
using Service.DTOs.FertilizerCalculatorDTOs.Responses;

namespace Service.Services.Interfaces;

public interface ICalculatorService
{
    Task<CalculatorResultResponse> CalculateAsync(CalculatorInputRequest request, CancellationToken cancellationToken = default);
}

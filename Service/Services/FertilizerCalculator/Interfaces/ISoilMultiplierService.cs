using Service.DTOs.FertilizerCalculatorDTOs.Requests;
using Service.DTOs.FertilizerCalculatorDTOs.Responses;

namespace Service.Services.FertilizerCalculator.Interfaces;

public interface ISoilMultiplierService
{
    Task<AddSoilMultiplierResponse> AddAsync(AddSoilMultiplierRequest request, CancellationToken cancellationToken = default);
    Task<List<AddSoilMultiplierResponse>> GetAllAsync(CancellationToken cancellationToken = default);
}
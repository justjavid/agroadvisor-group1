using Service.DTOs.FertilizerCalculatorDTOs.Requests;
using Service.DTOs.FertilizerCalculatorDTOs.Responses;

namespace Service.Services.Interfaces;

public interface ISoilMultiplierService
{
    Task<AddSoilMultiplierResponse> AddAsync(AddSoilMultiplierRequest request, CancellationToken cancellationToken = default);
}

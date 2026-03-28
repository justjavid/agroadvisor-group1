using Service.DTOs.FertilizerCalculatorDTOs.Requests;
using Service.DTOs.FertilizerCalculatorDTOs.Responses;

namespace Service.Services.Interfaces;

public interface IFertilizerService
{
    Task<AddFertilizerResponse> AddAsync(AddFertilizerRequest request, CancellationToken cancellationToken = default);
}

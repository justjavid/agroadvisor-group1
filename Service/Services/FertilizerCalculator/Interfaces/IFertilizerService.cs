using Service.DTOs.FertilizerCalculatorDTOs.Requests;
using Service.DTOs.FertilizerCalculatorDTOs.Responses;

namespace Service.Services.FertilizerCalculator.Interfaces;

public interface IFertilizerService
{
    Task<AddFertilizerResponse> AddAsync(AddFertilizerRequest request, CancellationToken cancellationToken = default);
    Task<List<AddFertilizerResponse>> GetAllAsync(CancellationToken cancellationToken = default);
}
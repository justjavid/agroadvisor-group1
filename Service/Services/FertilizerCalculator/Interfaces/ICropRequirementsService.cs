using Service.DTOs.FertilizerCalculatorDTOs.Requests;
using Service.DTOs.FertilizerCalculatorDTOs.Responses;

namespace Service.Services.Interfaces;

public interface ICropRequirementsService
{
    Task<AddCropRequirementsResponse> AddAsync(AddCropRequirementsRequest request, CancellationToken cancellationToken = default);
    Task<List<AddCropRequirementsResponse>> GetAllAsync(CancellationToken cancellationToken = default);
}

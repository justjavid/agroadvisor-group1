using System.Threading;
using System.Threading.Tasks;
using Service.DTOs.ImageAnalysisDTOs;

namespace Service.Services.Interfaces;

public interface IImageAnalysisService
{
    Task<ImageAnalysisResultDto> AnalyzeAsync(ImageAnalysisRequestDto request, CancellationToken cancellationToken = default);
}

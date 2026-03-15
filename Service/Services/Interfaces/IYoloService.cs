using System.Threading;
using System.Threading.Tasks;
using Service.DTOs.ImageAnalysisDTOs;

namespace Service.Services.Interfaces;

public interface IYoloService
{
    Task<IReadOnlyList<DiseaseRegionDto>> DetectDiseaseRegionsAsync(ImageAnalysisRequestDto request, CancellationToken cancellationToken = default);
}


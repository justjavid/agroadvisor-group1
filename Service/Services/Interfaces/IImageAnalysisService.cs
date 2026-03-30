using Service.DTOs.ImageAnalysisDTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Services.Interfaces
{
    public interface IImageAnalysisService
    {
        Task<AnalyzeImageResponseDto> AnalyzeImageAsync(AnalyzeImageRequestDto dto);
    }
}

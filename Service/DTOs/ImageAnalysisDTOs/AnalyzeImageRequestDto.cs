
namespace Service.DTOs.ImageAnalysisDTOs
{
    public class AnalyzeImageRequestDto
    {
        public string? ImageBase64 { get; set; }
        public string? Prompt { get; set; }
    }
}
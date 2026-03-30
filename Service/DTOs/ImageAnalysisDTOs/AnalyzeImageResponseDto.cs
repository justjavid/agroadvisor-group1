namespace Service.DTOs.ImageAnalysisDTOs
{
    public class AnalyzeImageResponseDto
    {
        public string PlantName { get; set; }
        public string DiseaseName { get; set; }
        public float Confidence { get; set; }
        public string? ImageBase64 { get; set; }
    }
}

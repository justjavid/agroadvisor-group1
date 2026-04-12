namespace Service.DTOs.ImageAnalysisDTOs
{
    public class AnalyzeImageResponseDto
    {
        public string PlantName { get; set; }
        public string DiseaseName { get; set; }
        public string Information { get; set; }
        public float Confidence { get; set; }
        public List<string> ImageUrls { get; set; } = new();
    }
}
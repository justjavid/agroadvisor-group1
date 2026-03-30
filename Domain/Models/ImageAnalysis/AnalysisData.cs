

namespace Domain.Models.ImageAnalysis
{
    public class AnalysisData
    {
        public int Id { get; set; }

        public string ImageUrl { get; set; }

        public string PlantName { get; set; }

        public string DiseaseName { get; set; }

        public float Confidence { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}

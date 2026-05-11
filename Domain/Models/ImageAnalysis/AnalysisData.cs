namespace Domain.Models.ImageAnalysis;

public class AnalysisData
{
    public int Id { get; set; }
    public Guid SessionId { get; set; }
    public string? UserId { get; set; }
    public string? Prompt { get; set; }
    public string? ImageUrl { get; set; }
    public string? PlantName { get; set; }
    public string? DiseaseName { get; set; }
    public float Confidence { get; set; }
    public string? AiResponseJson { get; set; }
    public DateTime CreatedDate { get; set; }
}

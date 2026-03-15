namespace Service.DTOs.ImageAnalysisDTOs;

public sealed class ImageAnalysisRequestDto
{
    public required byte[] ImageBytes { get; init; }
    public required string ContentType { get; init; }
    public string? PlantType { get; init; }
    public string? UserId { get; init; }
}

public sealed class DiseaseRegionDto
{
    // YOLO nəticələri üçün bounding-box koordinatları (0-1 aralığında normalizə edilmiş).
    public float X { get; init; }
    public float Y { get; init; }
    public float Width { get; init; }
    public float Height { get; init; }
    public float Confidence { get; init; }
}

public sealed record ImageAnalysisResultDto
{
    public string DiseaseName { get; init; } = string.Empty;
    public double Probability { get; init; }
    public string Description { get; init; } = string.Empty;
    public string[] Symptoms { get; init; } = [];
    public string Treatment { get; init; } = string.Empty;
    public string Prevention { get; init; } = string.Empty;

    // YOLO-dan gələn highlight məlumatları.
    public IReadOnlyList<DiseaseRegionDto> Regions { get; init; } = Array.Empty<DiseaseRegionDto>();

    public static ImageAnalysisResultDto TryParseFromModelContent(string content)
    {
        try
        {
            // Model cavabında yalnız JSON hissəsini götürməyə cəhd.
            var start = content.IndexOf('{');
            var end = content.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                var json = content.Substring(start, end - start + 1);
                var model = System.Text.Json.JsonSerializer.Deserialize<ModelResult>(json);
                if (model is not null)
                {
                    return new ImageAnalysisResultDto
                    {
                        DiseaseName = model.diseaseName ?? string.Empty,
                        Probability = model.probability,
                        Description = model.description ?? string.Empty,
                        Symptoms = model.symptoms ?? Array.Empty<string>(),
                        Treatment = model.treatment ?? string.Empty,
                        Prevention = model.prevention ?? string.Empty
                    };
                }
            }
        }
        catch
        {
            // ignore and fall back to generic result
        }

        return new ImageAnalysisResultDto
        {
            DiseaseName = "Naməlum xəstəlik",
            Probability = 0,
            Description = content,
            Symptoms = [],
            Treatment = "Dəqiq diaqnoz üçün mütləq yerli aqronomla məsləhətləşin.",
            Prevention = string.Empty
        };
    }

    private sealed class ModelResult
    {
        public string? diseaseName { get; init; }
        public double probability { get; init; }
        public string? description { get; init; }
        public string[]? symptoms { get; init; }
        public string? treatment { get; init; }
        public string? prevention { get; init; }
    }
}


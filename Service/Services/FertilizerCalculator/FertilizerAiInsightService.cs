using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Service.DTOs.FertilizerCalculatorDTOs.Responses;
using Service.Services.FertilizerCalculator.Interfaces;

namespace Service.Services.FertilizerCalculator;

public class FertilizerAiInsightService : IFertilizerAiInsightService
{
    private readonly HttpClient _httpClient;
    private readonly AiOptions _options;

    public FertilizerAiInsightService(HttpClient httpClient, AiOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<(string Summary, List<string> Suggestions)> GenerateInsightsAsync(
        CalculatorResultResponse result,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return FallbackInsights(result);
        }

        var prompt =
            $"Create insight for fertilizer plan. Crop: {result.CropType}, growth stage: {result.GrowthStage}, soil: {result.SoilType}, field size hectares: {result.FieldSizeHectares}, total N required: {result.TotalNRequired:F2}, total P required: {result.TotalPRequired:F2}, total K required: {result.TotalKRequired:F2}.";

        using var request = BuildRequest(prompt);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return FallbackInsights(result);
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseAiResponse(body, result, IsGeminiConfigured());
        }
        catch
        {
            return FallbackInsights(result);
        }
    }

    private HttpRequestMessage BuildRequest(string prompt)
    {
        if (IsGeminiConfigured())
        {
            return BuildGeminiRequest(prompt);
        }

        return BuildOpenAiCompatibleRequest(prompt);
    }

    private HttpRequestMessage BuildOpenAiCompatibleRequest(string prompt)
    {
        var payload = new
        {
            model = _options.Model,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "You are an agronomy assistant. Return strictly valid JSON only with keys summary and suggestions. suggestions must be a JSON array with 3 concise actionable items."
                },
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            temperature = 0.3
        };

        var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        return request;
    }

    private HttpRequestMessage BuildGeminiRequest(string prompt)
    {
        var endpoint = _options.Endpoint;
        if (endpoint.Contains("{model}", StringComparison.OrdinalIgnoreCase))
        {
            endpoint = endpoint.Replace("{model}", _options.Model, StringComparison.OrdinalIgnoreCase);
        }

        if (!endpoint.Contains("key=", StringComparison.OrdinalIgnoreCase))
        {
            endpoint += endpoint.Contains('?') ? $"&key={Uri.EscapeDataString(_options.ApiKey)}" : $"?key={Uri.EscapeDataString(_options.ApiKey)}";
        }

        var payload = new
        {
            systemInstruction = new
            {
                parts = new[]
                {
                    new
                    {
                        text = "You are an agronomy assistant. Return strictly valid JSON only with keys summary and suggestions. suggestions must be a JSON array with 3 concise actionable items."
                    }
                }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new
                        {
                            text = prompt
                        }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.3
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        return request;
    }

    private bool IsGeminiConfigured()
    {
        return _options.Endpoint.Contains("generativelanguage.googleapis.com", StringComparison.OrdinalIgnoreCase)
               || _options.Model.Contains("gemini", StringComparison.OrdinalIgnoreCase);
    }

    private static (string Summary, List<string> Suggestions) ParseAiResponse(
        string responseBody,
        CalculatorResultResponse result,
        bool isGemini)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var messageContent = isGemini
                ? doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString()
                : doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

            if (string.IsNullOrWhiteSpace(messageContent))
            {
                return FallbackInsights(result);
            }

            var jsonText = ExtractJsonObject(messageContent);
            using var contentDoc = JsonDocument.Parse(jsonText);
            var summary = contentDoc.RootElement.GetProperty("summary").GetString() ?? string.Empty;

            var suggestions = new List<string>();
            if (contentDoc.RootElement.TryGetProperty("suggestions", out var suggestionsElement)
                && suggestionsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in suggestionsElement.EnumerateArray())
                {
                    var value = item.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        suggestions.Add(value);
                    }
                }
            }

            return (summary, suggestions.Count > 0 ? suggestions : FallbackInsights(result).Suggestions);
        }
        catch
        {
            return FallbackInsights(result);
        }
    }

    private static string ExtractJsonObject(string content)
    {
        var cleaned = content.Trim();
        if (cleaned.StartsWith("```", StringComparison.Ordinal))
        {
            var firstBrace = cleaned.IndexOf('{');
            var lastBrace = cleaned.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                return cleaned[firstBrace..(lastBrace + 1)];
            }
        }

        return cleaned;
    }

    private static (string Summary, List<string> Suggestions) FallbackInsights(CalculatorResultResponse result)
    {
        var summary =
            $"For {result.FieldSizeHectares:F2} ha of {result.CropType} ({result.GrowthStage}), estimated nutrients are N {result.TotalNRequired:F2}, P {result.TotalPRequired:F2}, and K {result.TotalKRequired:F2}.";

        var highest = new[]
            {
                ("N", result.TotalNRequired),
                ("P", result.TotalPRequired),
                ("K", result.TotalKRequired)
            }
            .OrderByDescending(x => x.Item2)
            .First().Item1;

        var suggestions = new List<string>
        {
            $"Prioritize nutrient source planning for {highest} because it has the highest total requirement.",
            "Split fertilizer application into multiple doses to reduce nutrient loss and improve uptake efficiency.",
            "Validate with soil and leaf analysis before final application to avoid over- or under-fertilization."
        };

        return (summary, suggestions);
    }
}

public class AiOptions
{
    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-1.5-flash";
}

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Service.DTOs.FertilizerCalculatorDTOs.Responses;
using Service.Services.Interfaces;

namespace Service.Services;

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
            throw new InvalidOperationException("AI API key is not configured.");

        var prompt = $"""
            Aşağıdakı hesablanmış gübrələmə məlumatlarına əsaslanaraq konkret tövsiyə hazırla:

            Məhsul: {result.CropType}
            İnkişaf mərhələsi: {result.GrowthStage}
            Torpaq növü: {result.SoilType}
            Sahə ölçüsü: {result.FieldSizeHectares:F2} ha

            Hektara əsas qida tələbatı:
            - N (azot): {result.BaseNPerHectare:F2} kq/ha
            - P (fosfor): {result.BasePPerHectare:F2} kq/ha
            - K (kalium): {result.BaseKPerHectare:F2} kq/ha

            Torpaq/mərhələ düzəliş əmsalları:
            - N əmsalı: {result.AppliedNMultiplier:F2}
            - P əmsalı: {result.AppliedPMultiplier:F2}
            - K əmsalı: {result.AppliedKMultiplier:F2}

            Sahə üçün ümumi tələbat:
            - N (azot): {result.TotalNRequired:F2} kq
            - P (fosfor): {result.TotalPRequired:F2} kq
            - K (kalium): {result.TotalKRequired:F2} kq
            """;

        using var request = BuildRequest(prompt);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Gemini API error {(int)response.StatusCode}: {errorBody}");
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseAiResponse(body, IsGeminiConfigured());
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
                    content = "Sən aqronomiya köməkçisisən. Verilən hesablanmış dəyərlərə (kq, ha, əmsallar) istinad edərək konkret tövsiyə hazırla. Yalnız summary və suggestions açarları olan tam düzgün JSON qaytar. summary bir cümlə olmalı və ümumi tələbatı xülasə etməlidir. suggestions 3 maddədən ibarət JSON massiv olmalıdır; hər maddə konkret rəqəmlərə istinad etməli və bu məhsul, torpaq növü və inkişaf mərhələsi üçün praktik fəaliyyət tövsiyə etməlidir. Ümumi ifadələrdən çəkin. Bütün mətn Azərbaycan dilində olmalıdır."
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
                        text = "Sən aqronomiya köməkçisisən. Verilən hesablanmış dəyərlərə (kq, ha, əmsallar) istinad edərək konkret tövsiyə hazırla. Yalnız summary və suggestions açarları olan tam düzgün JSON qaytar. summary bir cümlə olmalı və ümumi tələbatı xülasə etməlidir. suggestions 3 maddədən ibarət JSON massiv olmalıdır; hər maddə konkret rəqəmlərə istinad etməli və bu məhsul, torpaq növü və inkişaf mərhələsi üçün praktik fəaliyyət tövsiyə etməlidir. Ümumi ifadələrdən çəkin. Bütün mətn Azərbaycan dilində olmalıdır."
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
        return _options.Endpoint.Contains("generativelanguage.googleapis.com", StringComparison.OrdinalIgnoreCase);
    }

    private static (string Summary, List<string> Suggestions) ParseAiResponse(string responseBody, bool isGemini)
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
            throw new InvalidOperationException("AI returned an empty response.");

        var jsonText = ExtractJsonObject(messageContent);
        using var contentDoc = JsonDocument.Parse(jsonText);
        var summary = contentDoc.RootElement.GetProperty("summary").GetString()
            ?? throw new InvalidOperationException("AI response missing 'summary' field.");

        var suggestions = new List<string>();
        if (contentDoc.RootElement.TryGetProperty("suggestions", out var suggestionsElement)
            && suggestionsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in suggestionsElement.EnumerateArray())
            {
                string? value = item.ValueKind == JsonValueKind.String
                    ? item.GetString()
                    : item.TryGetProperty("suggestion", out var s) ? s.GetString()
                    : item.TryGetProperty("recommendation", out var r) ? r.GetString()
                    : item.TryGetProperty("text", out var t) ? t.GetString()
                    : item.TryGetProperty("content", out var c) ? c.GetString()
                    : item.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                    suggestions.Add(value);
            }
        }

        if (suggestions.Count == 0)
            throw new InvalidOperationException("AI response missing 'suggestions' field.");

        return (summary, suggestions);
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

}

public class AiOptions
{
    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-1.5-flash";
}

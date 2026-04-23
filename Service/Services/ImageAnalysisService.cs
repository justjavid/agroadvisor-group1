using Domain.Models.ImageAnalysis;
using Microsoft.Extensions.Configuration;
using Repository.Repositories.Interfaces;
using Service.DTOs.ImageAnalysisDTOs;
using Service.Services.Interfaces;
using System.Text;
using System.Text.Json;

public class ImageAnalysisService : IImageAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public ImageAnalysisService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["GeminiSettings:ApiKey"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("Gemini API key is not configured. Set 'GeminiSettings:ApiKey' in configuration.");

    }


    public async Task<AnalyzeImageResponseDto> AnalyzeImageAsync(AnalyzeImageRequestDto dto)
    {
        var fullPrompt = dto.Prompt + @"
    Return ONLY JSON in this format and add promt answer to information:
    {
      ""plantName"": """",
      ""information"": """",
      ""diseaseName"": """",
      ""confidence"": 0.0
    }";

        var requestBody = new
        {
            contents = new[]
            {
            new
            {
                parts = new object[]
                {
                    new { text = fullPrompt },
                    new
                    {
                        inline_data = new
                        {
                            mime_type = "image/jpeg",
                            data = dto.ImageBase64
                        }
                    }
                }
            }
        }
        };

        var json = JsonSerializer.Serialize(requestBody);

        var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent?key={_apiKey}");

        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        // Retry transient errors (503, 429, 5xx) using simple exponential backoff
        const int maxAttempts = 3;
        int delayMs = 1000;
        HttpResponseMessage response = null!;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post,
                $"https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent?key={_apiKey}");

            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            response = await _httpClient.SendAsync(httpRequest);

            if (response.IsSuccessStatusCode)
                break;

            var statusCode = (int)response.StatusCode;

            if ((statusCode == 429 || statusCode == 503 || statusCode >= 500) && attempt < maxAttempts)
            {
                await Task.Delay(delayMs);
                delayMs *= 2;
                continue;
            }

            var errBody = await response.Content.ReadAsStringAsync();
            throw new Exception(errBody);
        }

        if (response == null || !response.IsSuccessStatusCode)
        {
            var errBody = response == null ? "No response from AI service" : await response.Content.ReadAsStringAsync();
            throw new Exception($"AI service unavailable after {maxAttempts} attempts. Last error: {errBody}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(responseContent);

        string? text = null;
        if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
            candidates.ValueKind == JsonValueKind.Array &&
            candidates.GetArrayLength() > 0)
        {
            var first = candidates[0];
            if (first.TryGetProperty("content", out var content) &&
                content.TryGetProperty("parts", out var parts) &&
                parts.ValueKind == JsonValueKind.Array && parts.GetArrayLength() > 0)
            {
                var part = parts[0];
                if (part.TryGetProperty("text", out var textProp) && textProp.ValueKind == JsonValueKind.String)
                {
                    text = textProp.GetString();
                }
            }
        }

        if (string.IsNullOrWhiteSpace(text))
            throw new Exception("AI returned empty text");

        // clean ```json``` block
        if (text.StartsWith("```"))
        {
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');

            if (start < 0 || end < 0 || end <= start)
                throw new Exception("Invalid Gemini JSON format");

            text = text.Substring(start, end - start + 1);
        }

        var result = JsonSerializer.Deserialize<AnalyzeImageResponseDto>(text,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (result == null)
            throw new Exception("Failed to parse Gemini response");

        return result;
    }
}
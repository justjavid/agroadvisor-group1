using Domain.Models.ImageAnalysis;
using Repository.Repositories.Interfaces;
using Service.DTOs.ImageAnalysisDTOs;
using Service.Services.Interfaces;
using System.Text;
using System.Text.Json;

public class ImageAnalysisService : IImageAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey = "AIzaSyAh3yC6jckq7wy2KjauH875kOaWwIgaXDI";

    public ImageAnalysisService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }


    public async Task<AnalyzeImageResponseDto> AnalyzeImageAsync(AnalyzeImageRequestDto dto)
    {
        // Gemini-yə düzgün JSON qaytarması üçün prompt
        var fullPrompt = dto.Prompt + @"
               Return ONLY JSON in this format:
               {
                 ""plantName"": """",
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

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }

        var responseContent = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(responseContent);

        if (!doc.RootElement.TryGetProperty("candidates", out var candidates))
            throw new Exception("No candidates in response");

        var text = candidates[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        //  CLEAN JSON
        if (text.StartsWith("```"))
        {
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            text = text.Substring(start, end - start + 1);
        }

        Console.WriteLine(text);

        //  DESERIALIZE SAFE
        var result = JsonSerializer.Deserialize<AnalyzeImageResponseDto>(text,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (result == null)
            throw new Exception("Failed to parse Gemini response");

        // Normalize empty imageBase64 to null so caller can easily check availability
        if (string.IsNullOrWhiteSpace(result.ImageBase64))
            result.ImageBase64 = null;

        return result;
    }
}
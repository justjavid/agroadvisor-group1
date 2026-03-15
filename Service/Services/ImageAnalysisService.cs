using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Service.DTOs.ImageAnalysisDTOs;
using Service.Services.Interfaces;

namespace Service.Services;

public class ImageAnalysisService : IImageAnalysisService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IYoloService _yoloService;

    public ImageAnalysisService(IHttpClientFactory httpClientFactory, IConfiguration configuration, IYoloService yoloService)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _yoloService = yoloService;
    }

    public async Task<ImageAnalysisResultDto> AnalyzeAsync(ImageAnalysisRequestDto request, CancellationToken cancellationToken = default)
    {
        // NOTE: This is a minimal implementation that calls OpenAI Vision.
        // You can switch to Plant.id or a dedicated YOLO service here.

        var apiKey = _configuration["OpenAI:ApiKey"];
        var model = _configuration["OpenAI:VisionModel"] ?? "gpt-4o-mini";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI:ApiKey configuration is missing.");
        }

        var client = _httpClientFactory.CreateClient("openai");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var systemPrompt =
            "Sən aqronom mütəxəssissən. Şəkilə əsasən bitki xəstəliyini təxmini diaqnoz et. " +
            "Nəticəni JSON formatında qaytar: { \"diseaseName\": \"string\", \"probability\": 0-1, " +
            "\"description\": \"string\", \"symptoms\": [\"string\"], \"treatment\": \"string\", \"prevention\": \"string\" }. " +
            "Mümkünsə ehtimal faizini real saxla, əmin deyilsənsə, bunu da qeyd et.";

        var payload = new
        {
            model,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = systemPrompt
                },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = $"Bitki növü: {request.PlantType ?? "bilinmir"}." },
                        new
                        {
                            type = "image_url",
                            image_url = new
                            {
                                url = $"data:{request.ContentType};base64,{Convert.ToBase64String(request.ImageBytes)}"
                            }
                        }
                    }
                }
            },
            temperature = 0.4
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        using var response = await client.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var content = json.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        // Try to parse JSON block from the model output.
        var result = ImageAnalysisResultDto.TryParseFromModelContent(content);

        // YOLO ilə xəstə regionların tapılması (əgər model varsa).
        var regions = await _yoloService.DetectDiseaseRegionsAsync(request, cancellationToken);
        result = result with { Regions = regions };

        return result;
    }
}

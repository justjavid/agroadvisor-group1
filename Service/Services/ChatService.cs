using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Service.DTOs.Chat;
using Service.Services.Interfaces;

namespace Service.Services;

public class ChatService : IChatService
{
    private const int DailyLimitPerUser = 15;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    // Sadə in-memory limit və tarixçə (demo üçün).
    private static readonly ConcurrentDictionary<(string UserId, DateOnly Day), int> DailyCounters = new();
    private static readonly ConcurrentDictionary<string, List<ChatMessageDto>> Histories = new();

    public ChatService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<ChatResponseDto> AskAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var key = (request.UserId, today);

        var current = DailyCounters.AddOrUpdate(key, 1, (_, old) => old + 1);
        if (current > DailyLimitPerUser)
        {
            throw new InvalidOperationException("Gündəlik 15 sorğu limiti keçilib.");
        }

        var history = Histories.GetOrAdd(request.UserId, _ => new List<ChatMessageDto>());
        history.Add(new ChatMessageDto { Role = "user", Content = request.Message });

        var apiKey = _configuration["OpenAI:ApiKey"];
        var model = _configuration["OpenAI:ChatModel"] ?? "gpt-4o-mini";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI:ApiKey configuration is missing.");
        }

        var client = _httpClientFactory.CreateClient("openai");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var systemPrompt =
            "Sən aqronom mütəxəssissən və fermerlərə məsləhət verirsən. " +
            "Cavablarını qısa, praktik və Azərbaycan dilində ver. " +
            "Hər cavabın sonunda mütləq bu cümləni əlavə et: " +
            "\"Bu ümumi məsləhətdir, yerli aqronomla məsləhətləşin.\"";

        var payload = new
        {
            model,
            messages = new List<object>
            {
                new { role = "system", content = systemPrompt }
            }.Concat(history.Select(m => new { role = m.Role, content = m.Content }))
             .Concat(new[]
             {
                 new { role = "user", content = request.Message }
             }),
            temperature = 0.5
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        using var response = await client.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var answer = json.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        var assistantMessage = new ChatMessageDto
        {
            Role = "assistant",
            Content = answer
        };

        history.Add(assistantMessage);

        return new ChatResponseDto
        {
            Answer = answer,
            History = history.ToArray()
        };
    }
}


using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Repository.Data;
using Domain.Models.ChatBot;
using Service.DTOs.Chat;
using Service.Services.Interfaces;

namespace Service.Services;

public class ChatService : IChatService
{
    private const int DailyLimitPerUser = 15;
    private const string DefaultGeminiModel = "gemini-2.5-flash";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ChatBotDbContext _context;

    // 🔹 In-memory limit (temporary)
    private static readonly ConcurrentDictionary<(string UserId, DateOnly Day), int> DailyCounters = new();

    public ChatService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ChatBotDbContext context)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _context = context;
    }

    public async Task<ChatResponseDto> AskAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.Message))
        {
            throw new InvalidOperationException("UserId və message boş ola bilməz.");
        }

        // ✅ LIMIT (hal-hazırda deaktivdir)
        /*
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var key = (request.UserId, today);

        var current = DailyCounters.AddOrUpdate(key, 1, (_, old) => old + 1);
        if (current > DailyLimitPerUser)
        {
            throw new InvalidOperationException("Gündəlik 15 sorğu limiti keçilib.");
        }
        */

        // ✅ SESSION CREATE
        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.ChatSessions.Add(session);

        // ✅ USER MESSAGE SAVE
        var userMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Role = "user",
            Content = request.Message,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.ChatMessages.Add(userMessage);

        var apiKey = _configuration["Gemini:ApiKey"];
        var baseUrl = _configuration["Gemini:BaseUrl"] ?? "https://generativelanguage.googleapis.com/";
        var model = _configuration["Gemini:Model"] ?? DefaultGeminiModel;

        string answer;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            answer = "AI konfiqurasiya olunmayıb.";
        }
        else
        {
            var client = _httpClientFactory.CreateClient();

            var systemPrompt =
                "Sən aqronomsan. Cavabları qısa və Azərbaycan dilində ver. " +
                "Sonda bu cümləni əlavə et: Bu ümumi məsləhətdir.";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = $"{systemPrompt}\n\nUser: {request.Message}" }
                        }
                    }
                }
            };

            var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"{baseUrl.TrimEnd('/')}/v1beta/models/{model}:generateContent?key={apiKey}")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                answer = "AI cavabı alınmadı. Sonra yenidən yoxla.";
            }
            else
            {
                var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                answer = ExtractText(json.RootElement);
            }
        }

        // ✅ ASSISTANT MESSAGE SAVE
        var assistantMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Role = "assistant",
            Content = answer,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.ChatMessages.Add(assistantMessage);

        // ✅ SAVE TO DB
        await _context.SaveChangesAsync(cancellationToken);

        return new ChatResponseDto
        {
            Answer = answer,
            History = new[]
            {
                new ChatMessageDto { Role = "user", Content = request.Message },
                new ChatMessageDto { Role = "assistant", Content = answer }
            }
        };
    }

    private static string ExtractText(JsonElement root)
    {
        if (!root.TryGetProperty("candidates", out var candidates))
            return "Boş cavab";

        var parts = candidates[0].GetProperty("content").GetProperty("parts");

        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var text))
            {
                return text.GetString() ?? "Boş cavab";
            }
        }

        return "Boş cavab";
    }
}
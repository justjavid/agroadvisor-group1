using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Repository.Data;
using Domain.Models.ChatBot;

using Service.DTOs.Chat;
using Service.Services.Interfaces;

public class ChatService : IChatService
{
    private const int DailyLimitPerUser = 15;

    private readonly ChatBotDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    private static readonly ConcurrentDictionary<(string, DateOnly), int> DailyCounters = new();

    public ChatService(
        ChatBotDbContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<ChatResponseDto> AskAsync(ChatRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.Message))
            throw new InvalidOperationException("UserId və message boş ola bilməz.");

        // 🔹 LIMIT (disabled)
        /*
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var key = (request.UserId, today);

        var current = DailyCounters.AddOrUpdate(key, 1, (_, old) => old + 1);
        if (current > DailyLimitPerUser)
            throw new InvalidOperationException("Gündəlik limit keçildi");
        */

        // 🔹 SESSION TAP / CREATE
        ChatSession session;

        if (request.SessionId.HasValue)
        {
            session = await _context.ChatSessions
                .FirstOrDefaultAsync(x => x.Id == request.SessionId.Value && x.UserId == request.UserId, cancellationToken)
                ?? throw new InvalidOperationException("Session tapılmadı");
        }
        else
        {
            session = new ChatSession
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId
            };

            _context.ChatSessions.Add(session);
        }

        // 🔹 USER MESSAGE
        _context.ChatMessages.Add(new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Role = ChatRole.User,
            Content = request.Message
        });

        var answer = await GetAiResponse(request.Message, cancellationToken);

        // 🔹 ASSISTANT MESSAGE
        _context.ChatMessages.Add(new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Role = ChatRole.Assistant,
            Content = answer
        });

        await _context.SaveChangesAsync(cancellationToken);

        var history = await GetSessionMessagesAsync(session.Id);

        return new ChatResponseDto
        {
            Answer = answer,
            SessionId = session.Id,
            History = history
        };
    }

    public async Task<IReadOnlyList<ChatMessageDto>> GetSessionMessagesAsync(Guid sessionId)
    {
        return await _context.ChatMessages
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new ChatMessageDto
            {
                Role = x.Role.ToString().ToLower(),
                Content = x.Content,
                CreatedAt = x.CreatedAtUtc
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Guid>> GetUserSessionsAsync(string userId)
    {
        return await _context.ChatSessions
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => x.Id)
            .ToListAsync();
    }

    public async Task DeleteSessionAsync(Guid sessionId, string userId)
    {
        var session = await _context.ChatSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId);

        if (session == null)
            throw new InvalidOperationException("Session tapılmadı");

        _context.ChatSessions.Remove(session);
        await _context.SaveChangesAsync();
    }

    private async Task<string> GetAiResponse(string message, CancellationToken cancellationToken)
    {
        var apiKey = _configuration["Gemini:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
            return "AI konfiqurasiya olunmayıb.";

        var client = _httpClientFactory.CreateClient();

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = message }
                    }
                }
            }
        };

        var response = await client.PostAsync(
            $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            return "AI cavabı alınmadı";

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

        return ExtractText(json.RootElement);
    }

    private static string ExtractText(JsonElement root)
    {
        if (!root.TryGetProperty("candidates", out var candidates))
            return "Boş cavab";

        var parts = candidates[0].GetProperty("content").GetProperty("parts");

        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var text))
                return text.GetString() ?? "Boş cavab";
        }

        return "Boş cavab";
    }
}
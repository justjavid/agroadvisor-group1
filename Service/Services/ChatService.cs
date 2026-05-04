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

        ChatSession session;

        // 🔹 SESSION TAP
        if (request.SessionId.HasValue)
        {
            session = await _context.ChatSessions
                .FirstOrDefaultAsync(x =>
                    x.Id == request.SessionId.Value &&
                    x.UserId == request.UserId &&
                    !x.IsDeleted,
                    cancellationToken)
                ?? throw new InvalidOperationException("Session tapılmadı");
        }
        else
        {
            session = new ChatSession
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Title = GenerateTitle(request.Message),
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.ChatSessions.Add(session);
        }

        // 🔹 USER MESSAGE
        _context.ChatMessages.Add(new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Role = ChatRole.User,
            Content = request.Message,
            CreatedAtUtc = DateTime.UtcNow
        });

        var answer = await GetAiResponse(request.Message, cancellationToken);

        // 🔹 ASSISTANT MESSAGE
        _context.ChatMessages.Add(new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Role = ChatRole.Assistant,
            Content = answer,
            CreatedAtUtc = DateTime.UtcNow
        });

        session.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new ChatResponseDto
        {
            Answer = answer,
            SessionId = session.Id,
            History = await GetSessionMessagesInternal(session.Id)
        };
    }

    // 🔐 INTERNAL (no user check needed)
    private async Task<IReadOnlyList<ChatMessageDto>> GetSessionMessagesInternal(Guid sessionId)
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

    // 🔐 PUBLIC (with user validation)
    public async Task<IReadOnlyList<ChatMessageDto>> GetSessionMessagesAsync(Guid sessionId, string userId)
    {
        var exists = await _context.ChatSessions
            .AnyAsync(x => x.Id == sessionId && x.UserId == userId && !x.IsDeleted);

        if (!exists)
            throw new InvalidOperationException("Session tapılmadı");

        return await GetSessionMessagesInternal(sessionId);
    }

    // 🔥 DTO qaytarırıq (important)
    public async Task<IReadOnlyList<ChatSessionDto>> GetUserSessionsAsync(string userId)
    {
        return await _context.ChatSessions
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .Select(x => new ChatSessionDto
            {
                Id = x.Id,
                Title = x.Title,
                CreatedAt = x.CreatedAtUtc,
                UpdatedAt = x.UpdatedAtUtc
            })
            .ToListAsync();
    }

    public async Task DeleteSessionAsync(Guid sessionId, string userId)
    {
        var session = await _context.ChatSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId);

        if (session == null)
            throw new InvalidOperationException("Session tapılmadı");

        session.IsDeleted = true;
        session.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    // 🔥 SMART TITLE
    private static string GenerateTitle(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "Yeni chat";

        var clean = message.Trim();

        return clean.Length <= 50
            ? clean
            : clean.Substring(0, 50) + "...";
    }

    // 🔥 ROBUST AI CALL
    private async Task<string> GetAiResponse(string message, CancellationToken cancellationToken)
    {
        try
        {
            var apiKey = _configuration["Gemini:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
                return "AI konfiqurasiya olunmayıb.";

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            return "AI cavabı gecikdi (timeout)";
        }
        catch
        {
            return "AI servisdə xəta baş verdi";
        }
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
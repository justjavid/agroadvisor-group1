using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Domain.Models.ChatBot;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Repository.Data;
using Service.DTOs.Chat;
using Service.Services.Interfaces;

public class ChatService : IChatService
{
    private const int DefaultDailyLimitPerUser = 15;
    private const int GeminiRetryCount = 3;
    private const int DefaultGeminiTimeoutSeconds = 25;

    private readonly ChatBotDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    private static readonly ConcurrentDictionary<(string UserId, DateOnly Date), int> DailyCounters = new();

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

        // ValidateDailyLimit(request.UserId);

        var session = await GetOrCreateSessionAsync(request, cancellationToken);

        _context.ChatMessages.Add(new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Role = ChatRole.User,
            Content = request.Message,
            CreatedAtUtc = DateTime.UtcNow
        });

        var answer = await GetAiResponse(request.Message, cancellationToken);

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
            History = await GetSessionMessagesInternalAsync(session.Id, cancellationToken)
        };
    }

    private async Task<ChatSession> GetOrCreateSessionAsync(ChatRequestDto request, CancellationToken cancellationToken)
    {
        if (request.SessionId.HasValue)
        {
            return await _context.ChatSessions
                .FirstOrDefaultAsync(x =>
                        x.Id == request.SessionId.Value &&
                        x.UserId == request.UserId &&
                        !x.IsDeleted,
                    cancellationToken)
                ?? throw new InvalidOperationException("Session tapılmadı");
        }

        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Title = GenerateTitle(request.Message),
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.ChatSessions.Add(session);
        return session;
    }

    private async Task<IReadOnlyList<ChatMessageDto>> GetSessionMessagesInternalAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
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
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChatMessageDto>> GetSessionMessagesAsync(
        Guid sessionId,
        string userId,
        CancellationToken cancellationToken)
    {
        var exists = await _context.ChatSessions
            .AnyAsync(x => x.Id == sessionId && x.UserId == userId && !x.IsDeleted, cancellationToken);

        if (!exists)
            throw new InvalidOperationException("Session tapılmadı");

        return await GetSessionMessagesInternalAsync(sessionId, cancellationToken);
    }

    public async Task<IReadOnlyList<ChatSessionDto>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken)
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
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteSessionAsync(Guid sessionId, string userId, CancellationToken cancellationToken)
    {
        var session = await _context.ChatSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId && !x.IsDeleted, cancellationToken);

        if (session == null)
            throw new InvalidOperationException("Session tapılmadı");

        session.IsDeleted = true;
        session.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateTitle(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "Yeni chat";

        var clean = message.Trim();

        return clean.Length <= 50
            ? clean
            : clean.Substring(0, 50) + "...";
    }

    private async Task<string> GetAiResponse(string message, CancellationToken cancellationToken)
    {
        try
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            var baseUrl = _configuration["Gemini:BaseUrl"] ?? "https://generativelanguage.googleapis.com/";
            var primaryModel = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";
            var fallbackModel = _configuration["Gemini:FallbackModel"];
            var timeoutSeconds = _configuration.GetValue<int?>("Gemini:TimeoutSeconds") ?? DefaultGeminiTimeoutSeconds;

            if (string.IsNullOrWhiteSpace(apiKey))
                return "AI konfiqurasiya olunmayıb.";

            var client = _httpClientFactory.CreateClient("gemini");
            client.Timeout = TimeSpan.FromSeconds(Math.Max(5, timeoutSeconds));

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

            var payloadJson = JsonSerializer.Serialize(payload);

            var modelsToTry = new List<string> { primaryModel };
            if (!string.IsNullOrWhiteSpace(fallbackModel) &&
                !string.Equals(fallbackModel, primaryModel, StringComparison.OrdinalIgnoreCase))
            {
                modelsToTry.Add(fallbackModel);
            }

            HttpResponseMessage? lastResponse = null;

            foreach (var model in modelsToTry)
            {
                lastResponse = await SendGeminiRequestWithRetryAsync(
                    client,
                    BuildGeminiEndpoint(baseUrl, model, apiKey),
                    payloadJson,
                    cancellationToken);

                if (lastResponse.IsSuccessStatusCode)
                {
                    break;
                }

                if (!IsTransientStatusCode((int)lastResponse.StatusCode))
                {
                    break;
                }
            }

            if (lastResponse is null)
            {
                return "AI cavabı alınmadı";
            }

            if (!lastResponse.IsSuccessStatusCode)
            {
                var errorBody = await lastResponse.Content.ReadAsStringAsync(cancellationToken);
                return BuildAiFailureMessage((int)lastResponse.StatusCode, errorBody);
            }

            var json = JsonDocument.Parse(await lastResponse.Content.ReadAsStringAsync(cancellationToken));

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

    private static string BuildGeminiEndpoint(string baseUrl, string model, string apiKey)
    {
        var normalizedBaseUrl = baseUrl.TrimEnd('/');
        return $"{normalizedBaseUrl}/v1beta/models/{model}:generateContent?key={apiKey}";
    }

    private static string BuildAiFailureMessage(int statusCode, string errorBody)
    {
        const int maxLength = 220;

        if (string.IsNullOrWhiteSpace(errorBody))
        {
            return $"AI cavabı alınmadı (HTTP {statusCode})";
        }

        var compactBody = errorBody.Replace("\r", string.Empty).Replace("\n", " ").Trim();
        if (compactBody.Length > maxLength)
        {
            compactBody = compactBody[..maxLength] + "...";
        }

        return $"AI cavabı alınmadı (HTTP {statusCode}): {compactBody}";
    }

    private static bool IsTransientStatusCode(int statusCode)
    {
        return statusCode is 408 or 429 or >= 500;
    }

    private static async Task<HttpResponseMessage> SendGeminiRequestWithRetryAsync(
        HttpClient client,
        string endpoint,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage? lastResponse = null;

        for (var attempt = 1; attempt <= GeminiRetryCount; attempt++)
        {
            lastResponse = await client.PostAsync(
                endpoint,
                new StringContent(payloadJson, Encoding.UTF8, "application/json"),
                cancellationToken);

            if (lastResponse.IsSuccessStatusCode || !IsTransientStatusCode((int)lastResponse.StatusCode))
            {
                return lastResponse;
            }

            if (attempt < GeminiRetryCount)
            {
                var delay = TimeSpan.FromMilliseconds(300 * attempt);
                await Task.Delay(delay, cancellationToken);
            }
        }

        return lastResponse!;
    }

    private void ValidateDailyLimit(string userId)
    {
        var dailyLimit = _configuration.GetValue<int?>("Chat:DailyLimitPerUser") ?? DefaultDailyLimitPerUser;
        if (dailyLimit <= 0)
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var key = (userId, today);

        var currentCount = DailyCounters.AddOrUpdate(key, 1, (_, previousCount) => previousCount + 1);
        if (currentCount > dailyLimit)
        {
            throw new InvalidOperationException("Gündəlik limit keçildi");
        }
    }
}
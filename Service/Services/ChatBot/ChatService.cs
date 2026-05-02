using System.Text;
using System.Text.Json;
using Domain.Models.ChatBot;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Service.DTOs.ChatBotDTOs;
using Service.Services.ChatBot.Interfaces;

namespace Service.Services.ChatBot;

public class ChatService : IChatService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _db;
    private readonly ChatAiOptions _options;

    public ChatService(HttpClient httpClient, AppDbContext db, ChatAiOptions options)
    {
        _httpClient = httpClient;
        _db = db;
        _options = options;
    }

    public async Task<ChatResponseDto> AskAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            throw new InvalidOperationException("Message is required.");

        var session = await GetOrCreateSessionAsync(request, cancellationToken);

        var userMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Role = ChatRole.User,
            Content = request.Message.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.ChatMessages.Add(userMessage);

        var priorMessages = await _db.ChatMessages
            .Where(x => x.SessionId == session.Id)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new ChatMessageDto
            {
                Role = x.Role.ToString().ToLower(),
                Content = x.Content,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var answer = await GenerateAnswerAsync(priorMessages, request.Message.Trim(), cancellationToken);

        var assistantMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Role = ChatRole.Assistant,
            Content = answer,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.ChatMessages.Add(assistantMessage);
        session.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var history = priorMessages
            .Append(new ChatMessageDto
            {
                Role = userMessage.Role.ToString().ToLower(),
                Content = userMessage.Content,
                CreatedAtUtc = userMessage.CreatedAtUtc
            })
            .Append(new ChatMessageDto
            {
                Role = assistantMessage.Role.ToString().ToLower(),
                Content = assistantMessage.Content,
                CreatedAtUtc = assistantMessage.CreatedAtUtc
            })
            .ToList();

        return new ChatResponseDto
        {
            SessionId = session.Id,
            Answer = answer,
            History = history
        };
    }

    public async Task<IReadOnlyList<ChatMessageDto>> GetSessionMessagesAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var sessionExists = await _db.ChatSessions.AnyAsync(x => x.Id == sessionId, cancellationToken);
        if (!sessionExists)
            throw new KeyNotFoundException("Chat session not found.");

        return await _db.ChatMessages
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new ChatMessageDto
            {
                Role = x.Role.ToString().ToLower(),
                Content = x.Content,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _db.ChatSessions
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteSessionAsync(Guid sessionId, string userId, CancellationToken cancellationToken = default)
    {
        var session = await _db.ChatSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, cancellationToken);

        if (session is null)
            throw new KeyNotFoundException("Chat session not found.");

        _db.ChatSessions.Remove(session);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<ChatSession> GetOrCreateSessionAsync(ChatRequestDto request, CancellationToken cancellationToken)
    {
        if (request.SessionId is Guid sessionId)
        {
            var existing = await _db.ChatSessions.FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
            if (existing is null)
                throw new InvalidOperationException("The provided sessionId does not exist.");

            return existing;
        }

        if (string.IsNullOrWhiteSpace(request.UserId))
            throw new InvalidOperationException("UserId is required when creating a new chat session.");

        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.ChatSessions.Add(session);
        return session;
    }

    private async Task<string> GenerateAnswerAsync(
        IReadOnlyList<ChatMessageDto> priorMessages,
        string latestMessage,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return "The chatbot is configured, but no AI API key is set yet. Add ChatAiOptions__ApiKey or AI_API_KEY to enable live responses.";

        using var request = BuildRequest(priorMessages, latestMessage);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                return $"The chatbot could not get an AI response right now. Provider error: {errorBody}";
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseAiResponse(responseBody, IsGeminiConfigured());
        }
        catch
        {
            return "The chatbot could not reach the AI provider right now. Please try again shortly.";
        }
    }

    private HttpRequestMessage BuildRequest(IReadOnlyList<ChatMessageDto> priorMessages, string latestMessage)
    {
        return IsGeminiConfigured()
            ? BuildGeminiRequest(priorMessages, latestMessage)
            : BuildOpenAiCompatibleRequest(priorMessages, latestMessage);
    }

    private HttpRequestMessage BuildOpenAiCompatibleRequest(IReadOnlyList<ChatMessageDto> priorMessages, string latestMessage)
    {
        var messages = new List<object>
        {
            new { role = "system", content = _options.SystemPrompt }
        };

        messages.AddRange(priorMessages.TakeLast(10).Select(x => new
        {
            role = x.Role,
            content = x.Content
        }));

        messages.Add(new { role = "user", content = latestMessage });

        var payload = new { model = _options.Model, messages, temperature = 0.4 };

        var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        return request;
    }

    private HttpRequestMessage BuildGeminiRequest(IReadOnlyList<ChatMessageDto> priorMessages, string latestMessage)
    {
        var endpoint = _options.Endpoint;
        if (endpoint.Contains("{model}", StringComparison.OrdinalIgnoreCase))
            endpoint = endpoint.Replace("{model}", _options.Model, StringComparison.OrdinalIgnoreCase);

        if (!endpoint.Contains("key=", StringComparison.OrdinalIgnoreCase))
            endpoint += endpoint.Contains('?') ? $"&key={Uri.EscapeDataString(_options.ApiKey)}" : $"?key={Uri.EscapeDataString(_options.ApiKey)}";

        var historyText = string.Join("\n", priorMessages.TakeLast(10).Select(x => $"{x.Role}: {x.Content}"));
        var prompt = string.IsNullOrWhiteSpace(historyText) ? latestMessage : $"{historyText}\nuser: {latestMessage}";

        // Prepend system prompt into the user turn since gemini-2.5-flash preview does not
        // accept systemInstruction as a top-level field via the REST v1 endpoint.
        var fullPrompt = string.IsNullOrWhiteSpace(_options.SystemPrompt)
            ? prompt
            : $"{_options.SystemPrompt}\n\n{prompt}";

        var payload = new
        {
            contents = new[] { new { role = "user", parts = new[] { new { text = fullPrompt } } } },
            generationConfig = new { temperature = 0.4 }
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

    private static string ParseAiResponse(string responseBody, bool isGemini)
    {
        using var doc = JsonDocument.Parse(responseBody);

        return isGemini
            ? doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "The AI returned an empty response."
            : doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "The AI returned an empty response.";
    }
}

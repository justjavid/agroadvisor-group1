using System;
using System.Collections.Concurrent;
using System.Net.Http;
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
    private const string DefaultGeminiModel = "gemini-1.5-flash";
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
        if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.Message))
        {
            throw new InvalidOperationException("UserId və message boş ola bilməz.");
        }

        // Daily limit is temporarily disabled for now.
        // var today = DateOnly.FromDateTime(DateTime.UtcNow);
        // var key = (request.UserId, today);
        //
        // var current = DailyCounters.AddOrUpdate(key, 1, (_, old) => old + 1);
        // if (current > DailyLimitPerUser)
        // {
        //     throw new InvalidOperationException("Gündəlik 15 sorğu limiti keçilib.");
        // }

        var history = Histories.GetOrAdd(request.UserId, _ => new List<ChatMessageDto>());
        var userMessage = new ChatMessageDto { Role = "user", Content = request.Message };
        history.Add(userMessage);

        var apiKey = _configuration["Gemini:ApiKey"];
        var baseUrl = _configuration["Gemini:BaseUrl"] ?? "https://generativelanguage.googleapis.com/";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            var fallbackAnswer =
                "Hazırda AI servisi konfiqurasiya olunmayıb. Gemini API açarını əlavə etdikdən sonra cavablar aktiv olacaq. " +
                "Bu ümumi məsləhətdir, yerli aqronomla məsləhətləşin.";
            var fallbackAssistant = new ChatMessageDto { Role = "assistant", Content = fallbackAnswer };
            history.Add(fallbackAssistant);
            return new ChatResponseDto
            {
                Answer = fallbackAnswer,
                History = history.ToArray()
            };
        }

        var client = _httpClientFactory.CreateClient();

        var systemPrompt =
            "Sən aqronom mütəxəssissən və fermerlərə məsləhət verirsən. " +
            "Cavablarını qısa, praktik və Azərbaycan dilində ver. " +
            "Hər cavabın sonunda mütləq bu cümləni əlavə et: " +
            "\"Bu ümumi məsləhətdir, yerli aqronomla məsləhətləşin.\"";

        var payload = new
        {
            contents = new List<object>
            {
                new
                {
                    parts = new[]
                    {
                        new
                        {
                            text = $"{systemPrompt}\n\n" +
                                   string.Join("\n", history.Select(m => $"{m.Role}: {m.Content}"))
                        }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.5
            }
        };

        using var response = await SendGeminiWithModelFallbackAsync(client, apiKey, baseUrl, payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var isQuotaOrRateLimit =
                (int)response.StatusCode == 429 ||
                (int)response.StatusCode == 403 ||
                body.Contains("insufficient_quota", StringComparison.OrdinalIgnoreCase) ||
                body.Contains("rate_limit", StringComparison.OrdinalIgnoreCase) ||
                body.Contains("resource_exhausted", StringComparison.OrdinalIgnoreCase) ||
                body.Contains("quota", StringComparison.OrdinalIgnoreCase) ||
                body.Contains("quota exceeded", StringComparison.OrdinalIgnoreCase) ||
                body.Contains("too many requests", StringComparison.OrdinalIgnoreCase);

            if (isQuotaOrRateLimit)
            {
                var fallbackAnswer =
                    "Hazırda AI servisi limit səbəbi ilə müvəqqəti əlçatan deyil. Zəhmət olmasa bir az sonra yenidən cəhd edin. " +
                    "Bu ümumi məsləhətdir, yerli aqronomla məsləhətləşin.";
                var fallbackAssistant = new ChatMessageDto { Role = "assistant", Content = fallbackAnswer };
                history.Add(fallbackAssistant);
                return new ChatResponseDto
                {
                    Answer = fallbackAnswer,
                    History = history.ToArray()
                };
            }

            throw new InvalidOperationException($"AI servisi xətası: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var answer = ExtractGeminiText(json.RootElement);
        if (string.IsNullOrWhiteSpace(answer))
        {
            answer = "Hazırda AI cavabı boş qaytarıldı. Bir az sonra yenidən cəhd edin.";
        }

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

    private static string ExtractGeminiText(JsonElement root)
    {
        if (!root.TryGetProperty("candidates", out var candidates) || candidates.ValueKind != JsonValueKind.Array || candidates.GetArrayLength() == 0)
        {
            return string.Empty;
        }

        var firstCandidate = candidates[0];
        if (!firstCandidate.TryGetProperty("content", out var content) || !content.TryGetProperty("parts", out var parts) || parts.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var texts = new List<string>();
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var textElement))
            {
                var text = textElement.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    texts.Add(text);
                }
            }
        }

        return string.Join("\n", texts);
    }

    private static async Task<HttpResponseMessage> SendGeminiWithModelFallbackAsync(
        HttpClient client,
        string apiKey,
        string baseUrl,
        object payload,
        CancellationToken cancellationToken)
    {
        var normalizedBaseUrl = baseUrl.TrimEnd('/');
        var modelCandidates = new List<string>
        {
            DefaultGeminiModel,
            "gemini-1.5-flash-latest",
            "gemini-2.0-flash",
            "gemini-1.5-pro"
        }.Distinct(StringComparer.OrdinalIgnoreCase);

        HttpResponseMessage? lastResponse = null;
        foreach (var candidate in modelCandidates)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{normalizedBaseUrl}/v1beta/models/{candidate}:generateContent?key={Uri.EscapeDataString(apiKey)}")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            if ((int)response.StatusCode != 404)
            {
                return response;
            }

            lastResponse?.Dispose();
            lastResponse = response;
        }

        return lastResponse ?? new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
    }
}


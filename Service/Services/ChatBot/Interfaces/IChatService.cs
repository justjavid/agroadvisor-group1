using Service.DTOs.ChatBotDTOs;

namespace Service.Services.ChatBot.Interfaces;

public interface IChatService
{
    Task<ChatResponseDto> AskAsync(string userId, ChatRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatMessageDto>> GetSessionMessagesAsync(string userId, Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default);
    Task DeleteSessionAsync(string userId, Guid sessionId, CancellationToken cancellationToken = default);
}

using Service.DTOs.ChatBotDTOs;

namespace Service.Services.ChatBot.Interfaces;

public interface IChatService
{
    Task<ChatResponseDto> AskAsync(ChatRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatMessageDto>> GetSessionMessagesAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default);
    Task DeleteSessionAsync(Guid sessionId, string userId, CancellationToken cancellationToken = default);
}

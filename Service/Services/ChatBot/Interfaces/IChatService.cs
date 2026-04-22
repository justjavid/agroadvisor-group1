using Service.DTOs.ChatBotDTOs;

namespace Service.Services.ChatBot.Interfaces;

public interface IChatService
{
    Task<ChatResponseDto> AskAsync(ChatRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatMessageDto>> GetHistoryAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

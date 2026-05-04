using System.Threading;
using System.Threading.Tasks;
using Service.DTOs.Chat;

namespace Service.Services.Interfaces;

public interface IChatService
{
    Task<ChatResponseDto> AskAsync(ChatRequestDto request, CancellationToken cancellationToken);

    Task<IReadOnlyList<ChatMessageDto>> GetSessionMessagesAsync(Guid sessionId, string userId);

    Task<IReadOnlyList<ChatSessionDto>> GetUserSessionsAsync(string userId);

    Task DeleteSessionAsync(Guid sessionId, string userId);
}
using System.Threading;
using System.Threading.Tasks;
using Service.DTOs.Chat;

namespace Service.Services.Interfaces;

public interface IChatService
{
    Task<ChatResponseDto> AskAsync(ChatRequestDto request, CancellationToken cancellationToken = default);
}


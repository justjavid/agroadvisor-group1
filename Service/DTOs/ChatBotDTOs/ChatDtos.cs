namespace Service.DTOs.ChatBotDTOs;

public sealed class ChatRequestDto
{
    public string? UserId { get; init; }
    public Guid? SessionId { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed class ChatMessageDto
{
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
}

public sealed class ChatResponseDto
{
    public Guid SessionId { get; init; }
    public string Answer { get; init; } = string.Empty;
    public IReadOnlyList<ChatMessageDto> History { get; init; } = [];
}

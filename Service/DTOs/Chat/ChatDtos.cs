namespace Service.DTOs.Chat;

public sealed class ChatRequestDto
{
    public required string UserId { get; init; }
    public Guid? SessionId { get; init; }
    public required string Message { get; init; }
}

public sealed class ChatMessageDto
{
    public required string Role { get; init; } 
    public required string Content { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class ChatResponseDto
{
    public required string Answer { get; init; }
    public required Guid SessionId { get; init; }
    public required IReadOnlyList<ChatMessageDto> History { get; init; }
}
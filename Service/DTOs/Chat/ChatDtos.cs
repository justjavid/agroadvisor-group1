namespace Service.DTOs.Chat;

public sealed class ChatRequestDto
{
    public required string UserId { get; init; }
    public required string Message { get; init; }
}

public sealed class ChatMessageDto
{
    public required string Role { get; init; } // "user" | "assistant" | "system"
    public required string Content { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public sealed class ChatResponseDto
{
    public required string Answer { get; init; }
    public required IReadOnlyList<ChatMessageDto> History { get; init; }
}


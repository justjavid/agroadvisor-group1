namespace Domain.Models.ChatBot;

public sealed class ChatMessage
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }
    public ChatSession? Session { get; set; }

    public ChatRole Role { get; set; }

    public required string Content { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}


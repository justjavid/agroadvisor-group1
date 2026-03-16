namespace Domain.Models.ChatBot;

public sealed class ChatMessage
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }
    public ChatSession? Session { get; set; }

    public required string Role { get; set; } // "user" | "assistant" | "system"

    public required string Content { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}


namespace Domain.Models.ChatBot;

public sealed class ChatMessage
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public ChatSession? Session { get; set; }
    public ChatRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

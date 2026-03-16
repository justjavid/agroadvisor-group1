namespace Domain.Models.ChatBot;

public sealed class ChatSession
{
    public Guid Id { get; set; }

    public required string UserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}


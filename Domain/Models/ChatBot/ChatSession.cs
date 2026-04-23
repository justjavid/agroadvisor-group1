namespace Domain.Models.ChatBot;

public sealed class ChatSession
{
    public Guid Id { get; set; }

    public required string UserId { get; set; }

    public string? Title { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; } = false;

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}


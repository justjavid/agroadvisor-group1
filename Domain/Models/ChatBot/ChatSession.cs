namespace Domain.Models.ChatBot;

public sealed class ChatSession
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? Title { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; } = false;
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}

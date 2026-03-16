using Domain.Models.ChatBot;
using Microsoft.EntityFrameworkCore;

namespace Repository.Data;

public sealed class ChatBotDbContext : DbContext
{
    public ChatBotDbContext(DbContextOptions<ChatBotDbContext> options) : base(options)
    {
    }

    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.ToTable("chat_sessions");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.UserId).IsRequired().HasMaxLength(128);
            entity.Property(x => x.CreatedAtUtc).IsRequired();

            entity.HasMany(x => x.Messages)
                .WithOne(x => x.Session)
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("chat_messages");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Role).IsRequired().HasMaxLength(32);
            entity.Property(x => x.Content).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();

            entity.HasIndex(x => new { x.SessionId, x.CreatedAtUtc });
        });
    }
}


using Domain.Models.Auth;
using Domain.Models.ChatBot;
using Domain.Models.FertilizerCalculator;
using Domain.Models.ImageAnalysis;
using Microsoft.EntityFrameworkCore;

namespace Repository.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<AnalysisData> AnalysisData => Set<AnalysisData>();
    public DbSet<CropRequirements> CropRequirements => Set<CropRequirements>();
    public DbSet<Fertilizer> Fertilizers => Set<Fertilizer>();
    public DbSet<SoilMultiplier> SoilMultiplier => Set<SoilMultiplier>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).IsRequired().HasMaxLength(128);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(256);
            entity.Property(x => x.UpdatedAtUtc);
            entity.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

            entity.HasMany(x => x.Messages)
                .WithOne(x => x.Session)
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Role)
                .IsRequired()
                .HasMaxLength(32)
                .HasConversion<string>();
            entity.Property(x => x.Content).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex(x => new { x.SessionId, x.CreatedAtUtc });
        });
    }
}

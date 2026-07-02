using Microsoft.EntityFrameworkCore;
using Orchi.Api.Entities;

namespace Orchi.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PipelineRun> PipelineRuns => Set<PipelineRun>();

    public DbSet<Chat> Chats => Set<Chat>();

    public DbSet<ChatMessageEntity> ChatMessages => Set<ChatMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Chat>(entity =>
        {
            entity.HasKey(chat => chat.Id);
            entity.Property(chat => chat.AgentId).HasMaxLength(64);
            entity.Property(chat => chat.WorkspacePath).HasMaxLength(2048);
            entity.Property(chat => chat.ExternalSessionId).HasMaxLength(256);
            entity.HasIndex(chat => chat.UpdatedAt);
            entity.HasQueryFilter(chat => !chat.IsDeleted);
        });

        modelBuilder.Entity<ChatMessageEntity>(entity =>
        {
            entity.HasKey(message => message.Id);
            entity.Property(message => message.Role).HasMaxLength(32);
            entity.Property(message => message.Status).HasMaxLength(32);
            entity.HasIndex(message => message.ChatId);
            entity.HasOne(message => message.Chat)
                .WithMany(chat => chat.Messages)
                .HasForeignKey(message => message.ChatId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

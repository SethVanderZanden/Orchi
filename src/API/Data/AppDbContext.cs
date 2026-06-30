using Microsoft.EntityFrameworkCore;
using Orchi.Api.Entities;

namespace Orchi.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PipelineRun> PipelineRuns => Set<PipelineRun>();

    public DbSet<Chat> Chats => Set<Chat>();

    public DbSet<ChatMessageEntity> ChatMessages => Set<ChatMessageEntity>();

    public DbSet<GoalJournalEntry> GoalJournalEntries => Set<GoalJournalEntry>();

    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<SubPlan> SubPlans => Set<SubPlan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Chat>(entity =>
        {
            entity.HasKey(chat => chat.Id);
            entity.Property(chat => chat.AgentId).HasMaxLength(64);
            entity.Property(chat => chat.WorkspacePath).HasMaxLength(2048);
            entity.Property(chat => chat.Mode).HasMaxLength(32);
            entity.Property(chat => chat.ExternalSessionId).HasMaxLength(256);
            entity.HasIndex(chat => chat.UpdatedAt);
            entity.HasIndex(chat => chat.ParentChatId);
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

        modelBuilder.Entity<GoalJournalEntry>(entity =>
        {
            entity.HasKey(entry => entry.Id);
            entity.HasIndex(entry => entry.ChatId);
            entity.HasOne(entry => entry.Chat)
                .WithMany(chat => chat.GoalJournal)
                .HasForeignKey(entry => entry.ChatId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Plan>(entity =>
        {
            entity.HasKey(plan => plan.Id);
            entity.Property(plan => plan.Title).HasMaxLength(512);
            entity.Property(plan => plan.Status).HasMaxLength(32);
            entity.HasIndex(plan => plan.SourceChatId);
        });

        modelBuilder.Entity<SubPlan>(entity =>
        {
            entity.HasKey(subPlan => subPlan.Id);
            entity.Property(subPlan => subPlan.Title).HasMaxLength(512);
            entity.Property(subPlan => subPlan.Status).HasMaxLength(32);
            entity.HasOne(subPlan => subPlan.Plan)
                .WithMany(plan => plan.SubPlans)
                .HasForeignKey(subPlan => subPlan.PlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

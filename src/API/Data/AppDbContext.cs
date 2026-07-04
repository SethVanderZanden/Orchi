using Microsoft.EntityFrameworkCore;
using Orchi.Api.Entities;

namespace Orchi.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PipelineRun> PipelineRuns => Set<PipelineRun>();

    public DbSet<Chat> Chats => Set<Chat>();

    public DbSet<ChatMessageEntity> ChatMessages => Set<ChatMessageEntity>();

    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<Workspace> Workspaces => Set<Workspace>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(project => project.Id);
            entity.Property(project => project.Name).HasMaxLength(256);
            entity.HasIndex(project => project.UpdatedAt);
        });

        modelBuilder.Entity<Workspace>(entity =>
        {
            entity.HasKey(workspace => workspace.Id);
            entity.Property(workspace => workspace.Path).HasMaxLength(2048);
            entity.Property(workspace => workspace.NormalizedPath).HasMaxLength(2048);
            entity.Property(workspace => workspace.Name).HasMaxLength(256);
            entity.HasIndex(workspace => new { workspace.ProjectId, workspace.NormalizedPath }).IsUnique();
            entity.HasOne(workspace => workspace.Project)
                .WithMany(project => project.Workspaces)
                .HasForeignKey(workspace => workspace.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Chat>(entity =>
        {
            entity.HasKey(chat => chat.Id);
            entity.Property(chat => chat.AgentId).HasMaxLength(64);
            entity.Property(chat => chat.WorkspacePath).HasMaxLength(2048);
            entity.Property(chat => chat.Mode).HasMaxLength(32);
            entity.Property(chat => chat.PlanFilePath).HasMaxLength(512);
            entity.Property(chat => chat.ExternalSessionId).HasMaxLength(256);
            entity.HasIndex(chat => chat.UpdatedAt);
            entity.HasIndex(chat => chat.ParentChatId);
            entity.HasIndex(chat => chat.ProjectId);
            entity.HasIndex(chat => chat.WorkspaceId);
            entity.HasOne(chat => chat.Project)
                .WithMany()
                .HasForeignKey(chat => chat.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(chat => chat.Workspace)
                .WithMany()
                .HasForeignKey(chat => chat.WorkspaceId)
                .OnDelete(DeleteBehavior.SetNull);
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

        modelBuilder.Entity<Plan>(entity =>
        {
            entity.HasKey(plan => new { plan.PlanId, plan.SourceChatId });
            entity.Property(plan => plan.PlanId).HasMaxLength(128);
            entity.Property(plan => plan.Title).HasMaxLength(512);
            entity.HasIndex(plan => plan.SourceChatId);
            entity.HasOne(plan => plan.SourceChat)
                .WithMany()
                .HasForeignKey(plan => plan.SourceChatId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

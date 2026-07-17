using Microsoft.EntityFrameworkCore;
using Orchi.Api.Entities;

namespace Orchi.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Chat> Chats => Set<Chat>();

    public DbSet<ChatMessageEntity> ChatMessages => Set<ChatMessageEntity>();

    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<Workspace> Workspaces => Set<Workspace>();

    public DbSet<AgentModel> AgentModels => Set<AgentModel>();

    public DbSet<AgentContextSize> AgentContextSizes => Set<AgentContextSize>();

    public DbSet<AgentCliOption> AgentCliOptions => Set<AgentCliOption>();

    public DbSet<ModeRuntimeDefault> ModeRuntimeDefaults => Set<ModeRuntimeDefault>();

    public DbSet<OrchestrationWorkflow> OrchestrationWorkflows => Set<OrchestrationWorkflow>();

    public DbSet<SelectionAction> SelectionActions => Set<SelectionAction>();

    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

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
            entity.Property(chat => chat.ModelId).HasMaxLength(256);
            entity.Property(chat => chat.ContextSizeId).HasMaxLength(64);
            entity.Property(chat => chat.ReasoningEffortId).HasMaxLength(64);
            entity.Property(chat => chat.ApprovalPolicyId).HasMaxLength(64);
            entity.Property(chat => chat.PlanFilePath).HasMaxLength(512);
            entity.Property(chat => chat.ExternalSessionId).HasMaxLength(256);
            entity.Property(chat => chat.Status).HasConversion<int>();
            entity.HasIndex(chat => chat.UpdatedAt);
            entity.HasIndex(chat => chat.Status);
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

        modelBuilder.Entity<AgentModel>(entity =>
        {
            entity.HasKey(model => new { model.AgentId, model.ModelId });
            entity.Property(model => model.AgentId).HasMaxLength(64);
            entity.Property(model => model.ModelId).HasMaxLength(256);
            entity.Property(model => model.Label).HasMaxLength(256);
            entity.Property(model => model.Source).HasMaxLength(16);
            entity.HasIndex(model => new { model.AgentId, model.IsEnabled });
        });

        modelBuilder.Entity<AgentContextSize>(entity =>
        {
            entity.HasKey(size => new { size.AgentId, size.SizeId });
            entity.Property(size => size.AgentId).HasMaxLength(64);
            entity.Property(size => size.SizeId).HasMaxLength(64);
            entity.Property(size => size.Label).HasMaxLength(256);
            entity.Property(size => size.Source).HasMaxLength(16);
            entity.HasIndex(size => new { size.AgentId, size.IsEnabled });
        });

        modelBuilder.Entity<AgentCliOption>(entity =>
        {
            entity.HasKey(option => new { option.AgentId, option.Kind, option.OptionId });
            entity.Property(option => option.AgentId).HasMaxLength(64);
            entity.Property(option => option.Kind).HasMaxLength(64);
            entity.Property(option => option.OptionId).HasMaxLength(64);
            entity.Property(option => option.Label).HasMaxLength(256);
            entity.Property(option => option.CliValue).HasMaxLength(256);
            entity.Property(option => option.Source).HasMaxLength(16);
            entity.HasIndex(option => new { option.AgentId, option.Kind, option.IsEnabled });
        });

        modelBuilder.Entity<ModeRuntimeDefault>(entity =>
        {
            entity.HasKey(row => row.Mode);
            entity.Property(row => row.Mode).HasMaxLength(32);
            entity.Property(row => row.AgentId).HasMaxLength(64);
            entity.Property(row => row.ModelId).HasMaxLength(256);
            entity.Property(row => row.ContextSizeId).HasMaxLength(64);
            entity.Property(row => row.ReasoningEffortId).HasMaxLength(64);
            entity.Property(row => row.ApprovalPolicyId).HasMaxLength(64);
        });

        modelBuilder.Entity<OrchestrationWorkflow>(entity =>
        {
            entity.HasKey(workflow => workflow.ParentChatId);
            entity.Property(workflow => workflow.Status).HasMaxLength(32);
            entity.Property(workflow => workflow.SequencePlanIdsJson).HasMaxLength(4096);
            entity.HasOne(workflow => workflow.ParentChat)
                .WithMany()
                .HasForeignKey(workflow => workflow.ParentChatId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SelectionAction>(entity =>
        {
            entity.HasKey(action => action.Id);
            entity.Property(action => action.Id).HasMaxLength(32);
            entity.Property(action => action.Label).HasMaxLength(128);
            entity.Property(action => action.Template).HasMaxLength(4000);
            entity.HasIndex(action => action.SortOrder);
        });

        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.HasKey(preference => preference.Id);
            entity.Property(preference => preference.Id).HasMaxLength(32);
            entity.Property(preference => preference.PostMessageBehavior).HasConversion<int>();
            entity.Property(preference => preference.EnabledAgentIdsJson).HasMaxLength(512);
        });
    }
}

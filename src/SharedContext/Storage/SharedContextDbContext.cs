using Microsoft.EntityFrameworkCore;
using Orchi.SharedContext.Storage.Entities;

namespace Orchi.SharedContext.Storage;

public sealed class SharedContextDbContext(DbContextOptions<SharedContextDbContext> options) : DbContext(options)
{
    public DbSet<WorkspaceEntity> Workspaces => Set<WorkspaceEntity>();

    public DbSet<IndexedFileEntity> IndexedFiles => Set<IndexedFileEntity>();

    public DbSet<SymbolEntity> Symbols => Set<SymbolEntity>();

    public DbSet<TaskSummaryEntity> TaskSummaries => Set<TaskSummaryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkspaceEntity>(entity =>
        {
            entity.ToTable("sc_Workspaces");
            entity.HasKey(workspace => workspace.Id);
            entity.Property(workspace => workspace.NormalizedPath).HasMaxLength(2048);
            entity.Property(workspace => workspace.GitBranch).HasMaxLength(256);
            entity.Property(workspace => workspace.GitHead).HasMaxLength(64);
            entity.HasIndex(workspace => workspace.NormalizedPath).IsUnique();
        });

        modelBuilder.Entity<IndexedFileEntity>(entity =>
        {
            entity.ToTable("sc_IndexedFiles");
            entity.HasKey(file => file.Id);
            entity.Property(file => file.RelativePath).HasMaxLength(2048);
            entity.Property(file => file.ContentHash).HasMaxLength(64);
            entity.Property(file => file.Language).HasMaxLength(32);
            entity.HasIndex(file => new { file.WorkspaceId, file.RelativePath }).IsUnique();
            entity.HasOne(file => file.Workspace)
                .WithMany(workspace => workspace.Files)
                .HasForeignKey(file => file.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SymbolEntity>(entity =>
        {
            entity.ToTable("sc_Symbols");
            entity.HasKey(symbol => symbol.Id);
            entity.Property(symbol => symbol.RelativePath).HasMaxLength(2048);
            entity.Property(symbol => symbol.Name).HasMaxLength(512);
            entity.Property(symbol => symbol.Kind).HasMaxLength(64);
            entity.Property(symbol => symbol.ParentSymbol).HasMaxLength(512);
            entity.HasIndex(symbol => new { symbol.WorkspaceId, symbol.RelativePath, symbol.Name, symbol.StartLine });
            entity.HasOne(symbol => symbol.Workspace)
                .WithMany(workspace => workspace.Symbols)
                .HasForeignKey(symbol => symbol.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskSummaryEntity>(entity =>
        {
            entity.ToTable("sc_TaskSummaries");
            entity.HasKey(summary => summary.Id);
            entity.Property(summary => summary.Status).HasMaxLength(32);
            entity.HasIndex(summary => new { summary.WorkspaceId, summary.ChatId }).IsUnique();
            entity.HasOne(summary => summary.Workspace)
                .WithMany(workspace => workspace.TaskSummaries)
                .HasForeignKey(summary => summary.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.ApplySqliteDateTimeOffsetConverters();
    }
}

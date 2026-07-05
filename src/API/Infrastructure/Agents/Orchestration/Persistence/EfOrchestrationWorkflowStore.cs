using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Orchi.Api.Data;
using Orchi.Api.Entities;

namespace Orchi.Api.Infrastructure.Agents.Orchestration.Persistence;

public sealed class EfOrchestrationWorkflowStore(IDbContextFactory<AppDbContext> dbContextFactory)
    : IOrchestrationWorkflowStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<OrchestrationWorkflowRecord?> GetAsync(
        Guid parentChatId,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        OrchestrationWorkflow? entity = await db.OrchestrationWorkflows
            .AsNoTracking()
            .FirstOrDefaultAsync(workflow => workflow.ParentChatId == parentChatId, cancellationToken);

        return entity is null ? null : ToRecord(entity);
    }

    public async Task UpsertAsync(OrchestrationWorkflowRecord record, CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        OrchestrationWorkflow? existing = await db.OrchestrationWorkflows
            .FirstOrDefaultAsync(workflow => workflow.ParentChatId == record.ParentChatId, cancellationToken);

        string sequenceJson = JsonSerializer.Serialize(record.SequencePlanIds, JsonOptions);

        if (existing is null)
        {
            db.OrchestrationWorkflows.Add(new OrchestrationWorkflow
            {
                ParentChatId = record.ParentChatId,
                Status = record.Status,
                SequencePlanIdsJson = sequenceJson,
                NextSequenceIndex = record.NextSequenceIndex,
                CreatedAt = record.CreatedAt == default ? now : record.CreatedAt,
                UpdatedAt = now
            });
        }
        else
        {
            existing.Status = record.Status;
            existing.SequencePlanIdsJson = sequenceJson;
            existing.NextSequenceIndex = record.NextSequenceIndex;
            existing.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static OrchestrationWorkflowRecord ToRecord(OrchestrationWorkflow entity)
    {
        IReadOnlyList<string> sequencePlanIds = JsonSerializer
            .Deserialize<List<string>>(entity.SequencePlanIdsJson, JsonOptions) ?? [];

        return new OrchestrationWorkflowRecord(
            entity.ParentChatId,
            entity.Status,
            sequencePlanIds,
            entity.NextSequenceIndex,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}

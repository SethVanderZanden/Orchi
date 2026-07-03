namespace Orchi.Api.Infrastructure.Agents.Plans.Persistence;

public sealed record PlanUpsertModel(
    string PlanId,
    Guid SourceChatId,
    string Title,
    string ContentMarkdown);

public sealed record StoredPlan(
    string PlanId,
    Guid SourceChatId,
    string Title,
    string ContentMarkdown,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public interface IPlanStore
{
    Task UpsertAsync(PlanUpsertModel model, CancellationToken cancellationToken);

    Task<StoredPlan?> GetAsync(Guid sourceChatId, string planId, CancellationToken cancellationToken);
}

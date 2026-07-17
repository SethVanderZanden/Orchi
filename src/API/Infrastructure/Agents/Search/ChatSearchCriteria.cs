using Orchi.Api.Entities;

namespace Orchi.Api.Infrastructure.Agents.Search;

/// <summary>
/// Criteria for chat search. Add optional fields here as new <see cref="IChatSearchClause"/>
/// implementations are introduced — the endpoint and composer stay stable.
/// </summary>
public sealed record ChatSearchCriteria(
    string? Query = null,
    int? Limit = null,
    Guid? ProjectId = null,
    ChatStatus? Status = null)
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 100;

    public int ResolveLimit()
    {
        if (Limit is null or <= 0)
        {
            return DefaultLimit;
        }

        return Math.Min(Limit.Value, MaxLimit);
    }

    public string? NormalizedQuery()
    {
        if (string.IsNullOrWhiteSpace(Query))
        {
            return null;
        }

        return Query.Trim();
    }
}

using Orchi.Api.Entities;

namespace Orchi.Api.Infrastructure.Agents.Search;

/// <summary>
/// Pluggable filter applied when building a chat search query.
/// Register new implementations in DI to extend search without changing the endpoint.
/// </summary>
public interface IChatSearchClause
{
    bool Applies(ChatSearchCriteria criteria);

    IQueryable<Chat> Apply(IQueryable<Chat> query, ChatSearchCriteria criteria);
}

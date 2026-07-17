using Orchi.Api.Entities;

namespace Orchi.Api.Infrastructure.Agents.Search;

public sealed class ChatSearchComposer(IEnumerable<IChatSearchClause> clauses)
{
    public IQueryable<Chat> Apply(IQueryable<Chat> query, ChatSearchCriteria criteria)
    {
        IQueryable<Chat> composed = query;

        foreach (IChatSearchClause clause in clauses)
        {
            if (!clause.Applies(criteria))
            {
                continue;
            }

            composed = clause.Apply(composed, criteria);
        }

        return composed;
    }
}

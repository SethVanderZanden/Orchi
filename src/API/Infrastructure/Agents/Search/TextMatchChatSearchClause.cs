using Orchi.Api.Entities;

namespace Orchi.Api.Infrastructure.Agents.Search;

/// <summary>
/// v1 text search: match message content, workspace path, or plan file path.
/// </summary>
public sealed class TextMatchChatSearchClause : IChatSearchClause
{
    public bool Applies(ChatSearchCriteria criteria) =>
        criteria.NormalizedQuery() is not null;

    public IQueryable<Chat> Apply(IQueryable<Chat> query, ChatSearchCriteria criteria)
    {
        string? term = criteria.NormalizedQuery();
        if (term is null)
        {
            return query;
        }

        return query.Where(chat =>
            chat.WorkspacePath.Contains(term) ||
            (chat.PlanFilePath != null && chat.PlanFilePath.Contains(term)) ||
            chat.Messages.Any(message => message.Content.Contains(term)));
    }
}

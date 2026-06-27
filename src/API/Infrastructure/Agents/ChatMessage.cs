namespace Orchi.Api.Infrastructure.Agents;

public sealed record ChatMessage(
    Guid Id,
    string Role,
    string Content,
    DateTimeOffset CreatedAt,
    string Status = "complete");

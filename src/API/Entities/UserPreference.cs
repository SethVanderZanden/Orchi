namespace Orchi.Api.Entities;

public enum PostMessageBehavior
{
    StayOnChat,
    GoToBoard,
    OpenNewChat
}

public class UserPreference
{
    public const string DefaultId = "default";

    public required string Id { get; set; }

    public PostMessageBehavior PostMessageBehavior { get; set; }

    /// <summary>
    /// JSON array of enabled agent ids (e.g. <c>["cursor","codex"]</c>).
    /// Empty array means the user has not configured agents yet.
    /// </summary>
    public string EnabledAgentIdsJson { get; set; } = "[]";

    public DateTimeOffset UpdatedAt { get; set; }
}

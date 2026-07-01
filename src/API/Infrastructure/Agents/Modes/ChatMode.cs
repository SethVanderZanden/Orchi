namespace Orchi.Api.Infrastructure.Agents.Modes;

public enum ChatMode
{
    Agent,
    Plan,
    Implement,
    Orchestrate,
    Goal,
    Participant
}

public static class ChatModeParser
{
    public static bool TryParse(string? value, out ChatMode mode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            mode = ChatMode.Agent;
            return true;
        }

        return Enum.TryParse(value, ignoreCase: true, out mode);
    }

    public static string ToApiString(ChatMode mode) =>
        mode switch
        {
            ChatMode.Agent => "agent",
            ChatMode.Plan => "plan",
            ChatMode.Implement => "implement",
            ChatMode.Orchestrate => "orchestrate",
            ChatMode.Goal => "goal",
            ChatMode.Participant => "participant",
            _ => "agent"
        };
}

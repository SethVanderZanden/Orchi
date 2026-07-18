namespace Orchi.Api.Entities;

public class ScriptBinding
{
    public required string Id { get; set; }

    public required string ScriptId { get; set; }

    public ScriptEventKind Event { get; set; }

    /// <summary>Null matches any mode; otherwise an <c>AgentModeIds</c> value.</summary>
    public string? ModeFilter { get; set; }

    public int Order { get; set; }

    public bool Enabled { get; set; } = true;

    public ScriptOnError OnError { get; set; } = ScriptOnError.Continue;

    public Script Script { get; set; } = null!;
}

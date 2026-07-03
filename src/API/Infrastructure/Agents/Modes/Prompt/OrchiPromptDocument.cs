namespace Orchi.Api.Infrastructure.Agents.Modes.Prompt;

public sealed class OrchiPromptDocument
{
    public string? Identity { get; set; }

    public string? Rules { get; set; }

    public string? Context { get; set; }

    public string? Tools { get; set; }

    public string? Task { get; set; }

    public string? Message { get; set; }

    public void AppendRules(string rules)
    {
        Rules = string.IsNullOrWhiteSpace(Rules)
            ? rules.Trim()
            : $"{Rules.Trim()}\n\n{rules.Trim()}";
    }

    public void AppendContext(string context)
    {
        Context = string.IsNullOrWhiteSpace(Context)
            ? context.Trim()
            : $"{Context.Trim()}\n\n{context.Trim()}";
    }
}

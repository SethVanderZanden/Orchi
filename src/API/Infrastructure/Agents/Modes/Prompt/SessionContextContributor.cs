namespace Orchi.Api.Infrastructure.Agents.Modes.Prompt;

public sealed class SessionContextContributor : IPromptSectionContributor
{
    public void Contribute(PromptBuildContext context, OrchiPromptDocument document)
    {
        if (string.IsNullOrWhiteSpace(context.WorkspacePath))
        {
            return;
        }

        document.AppendContext($"Workspace: {context.WorkspacePath.Trim()}");
    }
}

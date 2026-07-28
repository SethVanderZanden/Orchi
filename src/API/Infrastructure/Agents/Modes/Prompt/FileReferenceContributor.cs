namespace Orchi.Api.Infrastructure.Agents.Modes.Prompt;

public sealed class FileReferenceContributor : IPromptSectionContributor
{
    public void Contribute(PromptBuildContext context, OrchiPromptDocument document)
    {
        if (string.IsNullOrWhiteSpace(context.WorkspacePath))
        {
            return;
        }

        document.AppendRules(FileReferencePromptRules.Build(context.WorkspacePath));
    }
}

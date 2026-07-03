namespace Orchi.Api.Infrastructure.Agents.Modes.Prompt;

public sealed class PromptSectionPipeline(IEnumerable<IPromptSectionContributor> contributors)
{
    private readonly IReadOnlyList<IPromptSectionContributor> _contributors = contributors.ToList();

    public OrchiPromptDocument Build(PromptBuildContext context)
    {
        var document = new OrchiPromptDocument();

        foreach (IPromptSectionContributor contributor in _contributors)
        {
            contributor.Contribute(context, document);
        }

        return document;
    }
}

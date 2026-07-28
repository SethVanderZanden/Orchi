using Orchi.Api.Infrastructure.Agents.Attachments.Models;
using Orchi.Api.Infrastructure.Agents.Modes.Prompt;

namespace Orchi.Api.Infrastructure.Agents.Modes.Prompt;

/// <summary>
/// Injects user-uploaded attachment paths and optional text previews into the prompt context.
/// </summary>
public sealed class AttachmentContributor : IPromptSectionContributor
{
    public void Contribute(PromptBuildContext context, OrchiPromptDocument document)
    {
        if (context.AttachmentContext is not { PromptItems.Count: > 0 } attachmentContext)
        {
            return;
        }

        var lines = new List<string>
        {
            "The user attached the following files (materialized in the workspace):"
        };

        foreach (AttachmentPromptItem item in attachmentContext.PromptItems)
        {
            lines.Add(string.Empty);
            lines.Add($"- {item.FileName} → `{item.WorkspaceRelativePath}` ({item.ContentType})");

            if (item.IsImage)
            {
                lines.Add("  (image attached for visual inspection)");
            }
            else if (!string.IsNullOrWhiteSpace(item.ExtractedTextPreview))
            {
                lines.Add("  ```text");
                lines.Add(item.ExtractedTextPreview);
                lines.Add("  ```");
            }
        }

        document.AppendContext(string.Join('\n', lines));
    }
}

using System.Text;

namespace Orchi.Api.Infrastructure.Agents.Modes.Prompt;

public sealed class OrchiPromptRenderer
{
    private static readonly (string Tag, Func<OrchiPromptDocument, string?> GetValue)[] SectionOrder =
    [
        ("identity", document => document.Identity),
        ("rules", document => document.Rules),
        ("context", document => document.Context),
        ("tools", document => document.Tools),
        ("task", document => document.Task),
        ("message", document => document.Message),
    ];

    public string Render(OrchiPromptDocument document)
    {
        var builder = new StringBuilder();
        builder.Append("<orchi>");

        foreach ((string tag, Func<OrchiPromptDocument, string?> getValue) in SectionOrder)
        {
            string? value = getValue(document);
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            builder.Append('<').Append(tag).Append('>');
            builder.Append(FormatSectionBody(value.Trim()));
            builder.Append("</").Append(tag).Append('>');
        }

        builder.Append("</orchi>");
        return builder.ToString();
    }

    internal static string FormatSectionBody(string value) =>
        RequiresCData(value) ? $"<![CDATA[{value}]]>" : EscapeXml(value);

    private static bool RequiresCData(string value) =>
        value.Contains('<') || value.Contains('>') || value.Contains('&');

    private static string EscapeXml(string value) =>
        value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
}

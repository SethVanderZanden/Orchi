using System.Text.RegularExpressions;

namespace Orchi.Api.Infrastructure.SelectionActions;

public static partial class SelectionActionTemplate
{
    public const string SelectedTextPlaceholder = "{{selected text}}";

    public static bool ContainsSelectedTextPlaceholder(string template) =>
        SelectedTextPlaceholderRegex().IsMatch(template);

    public static string Apply(string template, string selectedText) =>
        SelectedTextPlaceholderRegex().Replace(template, selectedText);

    [GeneratedRegex(@"\{\{\s*selected text\s*\}\}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SelectedTextPlaceholderRegex();
}

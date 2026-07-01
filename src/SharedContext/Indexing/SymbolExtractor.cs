using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Orchi.SharedContext.Indexing;

internal static partial class SymbolExtractor
{
    private static readonly Regex CSharpType = TypeRegex();
    private static readonly Regex CSharpMethod = MethodRegex();
    private static readonly Regex TypeScriptDecl = TsDeclRegex();

    public static IReadOnlyList<SymbolIndexEntry> Extract(string relativePath, string content)
    {
        string language = FileLanguageDetector.Detect(relativePath);
        return language switch
        {
            "csharp" => ExtractCSharp(content),
            "typescript" or "javascript" => ExtractTypeScript(content),
            _ => []
        };
    }

    private static List<SymbolIndexEntry> ExtractCSharp(string content)
    {
        var symbols = new List<SymbolIndexEntry>();
        string[] lines = content.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            Match typeMatch = CSharpType.Match(line);
            if (typeMatch.Success)
            {
                symbols.Add(new SymbolIndexEntry(
                    typeMatch.Groups["name"].Value,
                    typeMatch.Groups["kind"].Value,
                    i + 1,
                    i + 1,
                    null));
            }

            Match methodMatch = CSharpMethod.Match(line);
            if (!methodMatch.Success)
            {
                continue;
            }

            symbols.Add(new SymbolIndexEntry(
                methodMatch.Groups["name"].Value,
                "method",
                i + 1,
                i + 1,
                null));
        }

        return symbols;
    }

    private static List<SymbolIndexEntry> ExtractTypeScript(string content)
    {
        var symbols = new List<SymbolIndexEntry>();
        string[] lines = content.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            Match match = TypeScriptDecl.Match(lines[i]);
            if (!match.Success)
            {
                continue;
            }

            symbols.Add(new SymbolIndexEntry(
                match.Groups["name"].Value,
                match.Groups["kind"].Value,
                i + 1,
                i + 1,
                null));
        }

        return symbols;
    }

    [GeneratedRegex(
        @"\b(?<kind>class|interface|struct|record|enum)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.Compiled)]
    private static partial Regex TypeRegex();

    [GeneratedRegex(
        @"\b(?:public|private|protected|internal|static|async|virtual|override|sealed|partial|\s)+[\w<>\[\],\s]+\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\(",
        RegexOptions.Compiled)]
    private static partial Regex MethodRegex();

    [GeneratedRegex(
        @"\b(?:export\s+)?(?:async\s+)?(?:function|class|interface|type|enum)\s+(?<name>[A-Za-z_$][\w$]*)",
        RegexOptions.Compiled)]
    private static partial Regex TsDeclRegex();
}

using System.Text;
using Microsoft.Extensions.Options;
using Orchi.SharedContext.Modes;
using Orchi.SharedContext.Storage;
using Orchi.SharedContext.Vectors;

namespace Orchi.SharedContext.Prompts;

internal sealed class ProjectRulesLoader
{
    private static readonly string[] RuleFileNames = ["AGENTS.md", "CLAUDE.md"];

    public string LoadStableProjectContext(string workspacePath)
    {
        var parts = new List<string>();

        foreach (string fileName in RuleFileNames)
        {
            string path = Path.Combine(workspacePath, fileName);
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                string content = File.ReadAllText(path).Trim();
                if (content.Length > 0)
                {
                    parts.Add($"## {fileName}\n\n{content}");
                }
            }
            catch
            {
            }
        }

        string cursorRules = LoadCursorRules(workspacePath);
        if (!string.IsNullOrWhiteSpace(cursorRules))
        {
            parts.Add(cursorRules);
        }

        string stack = DetectTechnologyStack(workspacePath);
        if (!string.IsNullOrWhiteSpace(stack))
        {
            parts.Add(stack);
        }

        if (parts.Count == 0)
        {
            return string.Empty;
        }

        return "## Project context\n\n" + string.Join("\n\n", parts);
    }

    private static string LoadCursorRules(string workspacePath)
    {
        string rulesDir = Path.Combine(workspacePath, ".cursor", "rules");
        if (!Directory.Exists(rulesDir))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.AppendLine("## Cursor rules");
        builder.AppendLine();

        foreach (string file in Directory.EnumerateFiles(rulesDir, "*.mdc", SearchOption.AllDirectories)
                     .Concat(Directory.EnumerateFiles(rulesDir, "*.md", SearchOption.AllDirectories)))
        {
            try
            {
                string content = File.ReadAllText(file).Trim();
                if (content.Length == 0)
                {
                    continue;
                }

                string relative = Path.GetRelativePath(workspacePath, file).Replace('\\', '/');
                builder.AppendLine($"### {relative}");
                builder.AppendLine();
                builder.AppendLine(content);
                builder.AppendLine();
            }
            catch
            {
            }
        }

        return builder.ToString().Trim();
    }

    private static string DetectTechnologyStack(string workspacePath)
    {
        var items = new List<string>();

        if (File.Exists(Path.Combine(workspacePath, "Orchi.Api.csproj")) ||
            Directory.EnumerateFiles(workspacePath, "*.csproj", SearchOption.AllDirectories).Any())
        {
            items.Add(".NET");
        }

        if (File.Exists(Path.Combine(workspacePath, "package.json")))
        {
            items.Add("Node.js");
        }

        if (items.Count == 0)
        {
            return string.Empty;
        }

        return $"## Technology stack\n\n{string.Join(", ", items)}";
    }
}

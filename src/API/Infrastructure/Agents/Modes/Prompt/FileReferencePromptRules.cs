namespace Orchi.Api.Infrastructure.Agents.Modes.Prompt;

public static class FileReferencePromptRules
{
    public static string Build(string workspacePath)
    {
        string path = workspacePath.Trim();

        return $$"""
            When you refer to a specific file location (path and line), include an open-editor element the UI can parse:

            <orchi-open-editor>code {{path}} -g {relativePath}:{line}</orchi-open-editor>

            - Use `code` as the CLI name (the app maps it to the user's editor).
            - Always use this exact workspace root: `{{path}}` — the workspace for this chat (including worktrees). Do not substitute a primary/main checkout or any other folder.
            - `{relativePath}` is the file path relative to that workspace root, using forward slashes.
            - `{line}` is the 1-based line number; add `:{column}` only when pointing at a specific column.
            - Place the element inline where you mention the location, or on its own line after a file heading.
            - Do not wrap the element in markdown code fences.
            """;
    }
}

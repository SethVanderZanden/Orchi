namespace Orchi.Api.Infrastructure.Agents.Modes.Prompt;

public static class FileReferencePromptRules
{
    public const string Rule = """
        When you refer to a specific file location (path and line), include an open-editor element the UI can parse:

        <orchi-open-editor>code {workspacePath} -g {relativePath}:{line}</orchi-open-editor>

        - Use `code` as the CLI name (the app maps it to the user's editor).
        - `{workspacePath}` is the workspace root from context (folder path or `.code-workspace` file when applicable).
        - `{relativePath}` is the file path relative to the workspace root, using forward slashes.
        - `{line}` is the 1-based line number; add `:{column}` only when pointing at a specific column.
        - Place the element inline where you mention the location, or on its own line after a file heading.
        - Do not wrap the element in markdown code fences.
        """;
}

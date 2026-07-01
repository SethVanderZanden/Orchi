namespace Orchi.SharedContext.Indexing;

internal static class FileLanguageDetector
{
    public static string Detect(string relativePath)
    {
        string extension = Path.GetExtension(relativePath).ToLowerInvariant();
        return extension switch
        {
            ".cs" => "csharp",
            ".ts" or ".tsx" => "typescript",
            ".js" or ".jsx" => "javascript",
            ".md" => "markdown",
            ".json" => "json",
            ".csproj" or ".sln" => "dotnet",
            _ => "text"
        };
    }
}

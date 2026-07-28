using System.Text;
using ExcelDataReader;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Orchi.Api.Infrastructure.Agents.Attachments;

internal static class AttachmentTextExtractor
{
    static AttachmentTextExtractor()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static bool IsExtractableDocument(string fileName, string contentType)
    {
        if (AttachmentPaths.IsImageContentType(contentType))
        {
            return false;
        }

        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        return contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
            || extension is ".csv" or ".txt" or ".md" or ".json" or ".xml" or ".yaml" or ".yml" or ".log"
            || AttachmentPaths.IsPdf(fileName, contentType)
            || AttachmentPaths.IsExcel(fileName, contentType);
    }

    public static string? Extract(string fileName, string contentType, string blobPath)
    {
        if (AttachmentPaths.IsPdf(fileName, contentType))
        {
            return ExtractPdf(blobPath);
        }

        if (AttachmentPaths.IsExcel(fileName, contentType))
        {
            return ExtractExcel(blobPath);
        }

        if (!IsPlainTextLike(fileName, contentType))
        {
            return null;
        }

        using FileStream stream = File.OpenRead(blobPath);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        string text = reader.ReadToEnd();
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private static bool IsPlainTextLike(string fileName, string contentType)
    {
        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        return contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
            || extension is ".csv" or ".txt" or ".md" or ".json" or ".xml" or ".yaml" or ".yml" or ".log";
    }

    private static string? ExtractPdf(string blobPath)
    {
        using PdfDocument document = PdfDocument.Open(blobPath);
        var builder = new StringBuilder();

        foreach (Page page in document.GetPages())
        {
            string pageText = page.Text;
            if (string.IsNullOrWhiteSpace(pageText))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine();
            }

            builder.Append(pageText.Trim());
        }

        return builder.Length == 0 ? null : builder.ToString();
    }

    private static string? ExtractExcel(string blobPath)
    {
        using FileStream stream = File.OpenRead(blobPath);
        using IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream);
        var builder = new StringBuilder();

        do
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.AppendLine($"## {reader.Name}");

            while (reader.Read())
            {
                var cells = new List<string>(reader.FieldCount);
                bool hasValue = false;

                for (int column = 0; column < reader.FieldCount; column++)
                {
                    object? value = reader.GetValue(column);
                    string text = value?.ToString()?.Trim() ?? string.Empty;
                    if (text.Length > 0)
                    {
                        hasValue = true;
                    }

                    cells.Add(text);
                }

                if (!hasValue)
                {
                    continue;
                }

                builder.AppendLine(string.Join('\t', cells));
            }
        }
        while (reader.NextResult());

        return builder.Length == 0 ? null : builder.ToString().Trim();
    }
}

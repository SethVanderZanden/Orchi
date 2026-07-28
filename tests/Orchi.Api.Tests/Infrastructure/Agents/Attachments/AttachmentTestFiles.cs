using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Orchi.Api.Tests.Infrastructure.Agents.Attachments;

internal static class AttachmentTestFiles
{
    public static async Task<string> WriteMinimalPdfAsync(string directory, string text = "Hello PDF")
    {
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "sample.pdf");
        string pdf = $"""
            %PDF-1.4
            1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj
            2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj
            3 0 obj<</Type/Page/MediaBox[0 0 612 792]/Parent 2 0 R/Resources<</Font<</F1 4 0 R>>>>/Contents 5 0 R>>endobj
            4 0 obj<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>endobj
            5 0 obj<</Length 44>>stream
            BT /F1 24 Tf 100 700 Td ({text}) Tj ET
            endstream
            endobj
            xref
            0 6
            0000000000 65535 f 
            0000000009 00000 n 
            0000000052 00000 n 
            0000000101 00000 n 
            0000000225 00000 n 
            0000000285 00000 n 
            trailer<</Size 6/Root 1 0 R>>
            startxref
            378
            %%EOF
            """;

        await File.WriteAllTextAsync(path, pdf);
        return path;
    }

    public static string WriteMinimalXlsx(string directory, params (string Sheet, string[][] Rows)[] sheets)
    {
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "sample.xlsx");
        using SpreadsheetDocument document = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook);
        WorkbookPart workbookPart = document.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();
        Sheets sheetsElement = workbookPart.Workbook.AppendChild(new Sheets());

        uint sheetId = 1;
        foreach ((string sheetName, string[][] rows) in sheets)
        {
            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();

            foreach (string[] rowValues in rows)
            {
                var row = new Row();
                foreach (string value in rowValues)
                {
                    row.Append(new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(value)
                    });
                }

                sheetData.Append(row);
            }

            worksheetPart.Worksheet = new Worksheet(sheetData);
            sheetsElement.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = sheetId++,
                Name = sheetName
            });
        }

        workbookPart.Workbook.Save();
        return path;
    }
}

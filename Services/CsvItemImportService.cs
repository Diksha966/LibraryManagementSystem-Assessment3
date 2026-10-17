using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services;

public class ImportRow
{
    public int RowNumber { get; set; }          // spreadsheet row (header = 1)
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string LibraryCode { get; set; } = string.Empty;
    public Item? Item { get; set; }
    public List<string> Errors { get; } = new();
    public bool IsValid => Errors.Count == 0 && Item != null;
}

public class ImportPreview
{
    public List<ImportRow> Rows { get; } = new();
    public List<string> FileErrors { get; } = new();
    public int ValidCount => Rows.Count(r => r.IsValid);
    public int ErrorCount => Rows.Count(r => !r.IsValid);
}

/// <summary>
/// Parses and validates a CSV of library items and imports the valid rows.
/// No external packages are required.
/// </summary>
public class CsvItemImportService
{
    public const int MaxRows = 500;

    public static readonly string[] Headers =
    {
        "Type", "Title", "Description", "LibraryCode",
        "Author", "Genre", "ISBN",
        "ToyType", "RecommendedAge",
        "Artist", "Year", "Format",
        "BranchCode"
    };

    public const string TemplateCsv =
        "Type,Title,Description,LibraryCode,Author,Genre,ISBN,ToyType,RecommendedAge,Artist,Year,Format,BranchCode\r\n" +
        "Book,Dune,A science fiction epic set on the desert planet Arrakis.,BK100,Frank Herbert,Science Fiction,9780441172719,,,,,,CEN\r\n" +
        "Toy,Wooden Train Set,A 40 piece wooden railway set.,TY100,,,,Building,4,,,,CEN\r\n" +
        "Music,Kind of Blue,A landmark jazz album.,MU100,,,,,,Miles Davis,1959,CD,NTH\r\n";

    private readonly LibraryDbContext _context;

    public CsvItemImportService(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<ImportPreview> ParseAsync(string csvText)
    {
        var preview = new ImportPreview();

        if (string.IsNullOrWhiteSpace(csvText))
        {
            preview.FileErrors.Add("The file is empty.");
            return preview;
        }

        if (csvText[0] == '\uFEFF')
            csvText = csvText.Substring(1);

        var records = ParseCsv(csvText);

        if (records.Count == 0)
        {
            preview.FileErrors.Add("The file is empty.");
            return preview;
        }

        // Map header names (case-insensitive) to column positions.
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < records[0].Count; i++)
        {
            string name = records[0][i].Trim();

            if (name.Length > 0 && !columns.ContainsKey(name))
                columns[name] = i;
        }

        foreach (var required in new[] { "Type", "Title", "Description", "LibraryCode" })
        {
            if (!columns.ContainsKey(required))
                preview.FileErrors.Add($"Missing required column: {required}.");
        }

        if (preview.FileErrors.Count > 0)
            return preview;

        var dataRecords = records.Skip(1)
            .Select((cells, index) => new { cells, RowNumber = index + 2 })
            .Where(x => x.cells.Any(c => !string.IsNullOrWhiteSpace(c)))
            .ToList();

        if (dataRecords.Count == 0)
        {
            preview.FileErrors.Add("The file has a header row but no items.");
            return preview;
        }

        if (dataRecords.Count > MaxRows)
        {
            preview.FileErrors.Add(
                $"The file has {dataRecords.Count} rows. The maximum is {MaxRows} per import.");
            return preview;
        }

        var existingCodes = (await _context.Items
                .Select(i => i.LibraryCode)
                .ToListAsync())
            .Select(c => c.Trim().ToUpperInvariant())
            .ToHashSet();

        var codesInFile = new Dictionary<string, int>();

        // Branch codes (for example CEN, NTH). A blank BranchCode uses the
        // first active branch, like the Add Item screen does.
        var branchesByCode = (await _context.Branches
                .Where(b => b.IsActive)
                .OrderBy(b => b.Id)
                .ToListAsync())
            .GroupBy(b => b.Code.ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        int defaultBranchId = branchesByCode.Values
            .Select(b => b.Id)
            .DefaultIfEmpty(1)
            .Min();

        foreach (var record in dataRecords)
        {
            var cells = record.cells;

            string Get(string name) =>
                columns.TryGetValue(name, out int idx) && idx < cells.Count
                    ? cells[idx].Trim()
                    : string.Empty;

            var row = new ImportRow
            {
                RowNumber = record.RowNumber,
                Type = Get("Type"),
                Title = Get("Title"),
                LibraryCode = Get("LibraryCode")
            };

            Item? item = null;

            switch (row.Type.ToLowerInvariant())
            {
                case "book":
                    item = new Book
                    {
                        Author = Get("Author"),
                        Genre = Get("Genre"),
                        ISBN = Get("ISBN")
                    };
                    break;

                case "toy":
                    item = new Toy { ToyType = Get("ToyType") };

                    if (TryReadInt(Get("RecommendedAge"), out int age, out bool ageBad))
                        ((Toy)item).RecommendedAge = age;
                    else if (ageBad)
                        row.Errors.Add("RecommendedAge must be a whole number.");
                    break;

                case "music":
                    item = new Music
                    {
                        Artist = Get("Artist"),
                        Format = Get("Format")
                    };

                    if (TryReadInt(Get("Year"), out int year, out bool yearBad))
                        ((Music)item).Year = year;
                    else if (yearBad)
                        row.Errors.Add("Year must be a whole number.");
                    break;

                default:
                    row.Errors.Add("Type must be Book, Toy or Music.");
                    break;
            }

            if (item != null)
            {
                item.Title = row.Title;
                item.Description = Get("Description");
                item.LibraryCode = row.LibraryCode;
                item.Status = ItemStatus.Available;
                item.BranchId = defaultBranchId;

                string branchCode = Get("BranchCode");

                if (branchCode.Length > 0)
                {
                    if (branchesByCode.TryGetValue(branchCode.ToUpperInvariant(), out var branch))
                        item.BranchId = branch.Id;
                    else
                        row.Errors.Add("BranchCode \"" + branchCode + "\" is not an active branch.");
                }

                // Reuse the model's data annotations (Required, StringLength, Range).
                var results = new List<ValidationResult>();

                Validator.TryValidateObject(
                    item,
                    new ValidationContext(item),
                    results,
                    validateAllProperties: true);

                foreach (var result in results)
                {
                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                        row.Errors.Add(result.ErrorMessage);
                }

                row.Item = item;
            }

            string codeKey = row.LibraryCode.ToUpperInvariant();

            if (codeKey.Length > 0)
            {
                if (existingCodes.Contains(codeKey))
                {
                    row.Errors.Add("This library code already exists in the catalogue.");
                }
                else if (codesInFile.TryGetValue(codeKey, out int firstRow))
                {
                    row.Errors.Add($"Duplicate library code (also in row {firstRow}).");
                }
                else
                {
                    codesInFile[codeKey] = row.RowNumber;
                }
            }

            preview.Rows.Add(row);
        }

        return preview;
    }

    // Re-parses (the catalogue may have changed) and saves the valid rows.
    public async Task<(int Imported, List<ImportRow> Skipped)> ImportAsync(string csvText)
    {
        var preview = await ParseAsync(csvText);

        var valid = preview.Rows.Where(r => r.IsValid).ToList();

        foreach (var row in valid)
            _context.Items.Add(row.Item!);

        if (valid.Count > 0)
            await _context.SaveChangesAsync();

        return (valid.Count, preview.Rows.Where(r => !r.IsValid).ToList());
    }

    private static bool TryReadInt(string text, out int value, out bool invalid)
    {
        value = 0;
        invalid = false;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            return true;

        invalid = true;
        return false;
    }

    // Minimal RFC 4180 parser: quoted fields, escaped quotes, commas and
    // line breaks inside quotes.
    private static List<List<string>> ParseCsv(string text)
    {
        var records = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if (c == '\r')
            {
                // ignored; the following \n ends the row
            }
            else if (c == '\n')
            {
                row.Add(field.ToString());
                field.Clear();
                records.Add(row);
                row = new List<string>();
            }
            else
            {
                field.Append(c);
            }
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            records.Add(row);
        }

        return records;
    }
}

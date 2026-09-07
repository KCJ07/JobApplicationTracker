using System.Globalization;
using ClosedXML.Excel;
using JobApplicationTracker.Data;
using JobApplicationTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace JobApplicationTracker.Services
{
    public class ExcellImportService : IExcellImportService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public ExcellImportService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        /// <summary>Reads the header row plus the first N data rows for the mapping UI.</summary>
        public async Task<ExcelPreview> PreviewAsync(Stream file, int sampleRows = 10)
        {
            using var wb = await OpenWorkbookAsync(file);
            var range = wb.Worksheets.First().RangeUsed();
            if (range is null)
                return new ExcelPreview();

            int colCount = range.ColumnCount();
            int headerRowNum = FindHeaderRow(range);
            var headerRow = range.Row(headerRowNum);

            var headers = new List<string>();
            for (int c = 1; c <= colCount; c++)
                headers.Add(CellText(headerRow.Cell(c)));

            var rows = new List<Dictionary<string, string>>();
            int lastRow = Math.Min(range.RowCount(), headerRowNum + sampleRows);
            for (int r = headerRowNum + 1; r <= lastRow; r++)
            {
                var xlRow = range.Row(r);
                var dict = new Dictionary<string, string>();
                bool anyValue = false;
                for (int c = 1; c <= colCount; c++)
                {
                    var v = CellText(xlRow.Cell(c));
                    if (!string.IsNullOrWhiteSpace(headers[c - 1]))
                        dict[headers[c - 1]] = v;
                    if (v.Length > 0)
                        anyValue = true;
                }
                if (anyValue)
                    rows.Add(dict);
            }

            return new ExcelPreview
            {
                Headers = headers.Where(h => h.Length > 0).ToList(),
                SampleRows = rows,
                TotalDataRows = range.RowCount() - headerRowNum,
            };
        }

        /// <summary>Imports every data row, using the user's confirmed column mappings.</summary>
        public async Task<int> ImportAsync(Stream file, IReadOnlyList<ColumnMapping> mappings, string userId)
        {
            using var wb = await OpenWorkbookAsync(file);
            var range = wb.Worksheets.First().RangeUsed();
            if (range is null)
                return 0;

            // header text -> 1-based column number within the used range
            int colCount = range.ColumnCount();
            int headerRowNum = FindHeaderRow(range);
            if (range.RowCount() <= headerRowNum)
                return 0;

            var headerRow = range.Row(headerRowNum);
            var columnByHeader = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int c = 1; c <= colCount; c++)
            {
                var name = CellText(headerRow.Cell(c));
                if (!string.IsNullOrEmpty(name))
                    columnByHeader[name] = c;
            }

            var active = mappings
                .Where(m => !string.IsNullOrWhiteSpace(m.TargetField)
                            && columnByHeader.ContainsKey(m.SourceColumnName))
                .ToList();
            if (active.Count == 0)
                return 0;

            var applications = new List<Application>();

            for (int r = headerRowNum + 1; r <= range.RowCount(); r++)
            {
                var xlRow = range.Row(r);
                string Value(ColumnMapping m) => CellText(xlRow.Cell(columnByHeader[m.SourceColumnName]));

                if (active.All(m => string.IsNullOrWhiteSpace(Value(m))))
                    continue; // skip blank rows

                var job = new Job();
                var app = new Application
                {
                    ApplicationUserId = userId,
                    Status = ApplicationStatus.Applied,
                    HeardBack = false,
                    DateApplied = DateOnly.FromDateTime(DateTime.Today),
                    ReachOutDate = DateOnly.FromDateTime(DateTime.Today),
                    Job = job,
                };

                foreach (var m in active)
                {
                    var v = Value(m);
                    switch (m.TargetField)
                    {
                        case "JobTitle":          job.JobTitle = v;          break;
                        case "Company":           job.Company = v;           break;
                        case "Website":           job.Website = v;           break;
                        case "State":             job.State = v;             break;
                        case "Description":       job.Description = v;       break;
                        case "LinkedlnRecruiter": job.LinkedlnRecruiter = v; break;
                        case "AppType":
                            if (Enum.TryParse<ApplicationType>(v, ignoreCase: true, out var appType))
                                job.AppType = appType;
                            break;
                        case "Notes":             app.Notes = v;            break;
                        case "Status":
                            if (Enum.TryParse<ApplicationStatus>(v, ignoreCase: true, out var status))
                                app.Status = status;
                            break;
                        case "HeardBack":         app.HeardBack = ParseYesNo(v); break;
                        case "DateApplied":
                            if (TryParseDate(v, out var applied))
                                app.DateApplied = applied;
                            break;
                        case "ReachOutDate":
                            if (TryParseDate(v, out var reachOut))
                                app.ReachOutDate = reachOut;
                            break;
                    }
                }

                applications.Add(app);
            }

            await using var context = await _contextFactory.CreateDbContextAsync();
            context.Applications.AddRange(applications);
            await context.SaveChangesAsync();
            return applications.Count;
        }

        // The header row isn't always the first row — sheets often have a banner/title above it.
        // Pick the row with the most non-empty cells among the first 10 rows of the used range.
        private static int FindHeaderRow(IXLRange range)
        {
            int scan = Math.Min(range.RowCount(), 10);
            int bestRow = 1, bestCount = -1;
            for (int r = 1; r <= scan; r++)
            {
                var row = range.Row(r);
                int count = 0;
                for (int c = 1; c <= range.ColumnCount(); c++)
                    if (!string.IsNullOrWhiteSpace(row.Cell(c).GetString()))
                        count++;
                if (count > bestCount)
                {
                    bestCount = count;
                    bestRow = r;
                }
            }
            return bestRow;
        }

        // Excel date cells come back from GetString() as "M/d/yyyy h:mm:ss AM", which
        // DateOnly.TryParse rejects. Normalize real dates to ISO so parsing (and the
        // preview) get a clean value; everything else is read as trimmed text.
        private static string CellText(IXLCell cell)
        {
            if (cell.DataType == XLDataType.DateTime && cell.TryGetValue<DateTime>(out var dt))
                return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return cell.GetString().Trim();
        }

        internal static bool ParseYesNo(string v) =>
            v.Trim().ToLowerInvariant() is "yes" or "y" or "true" or "1" or "x";

        internal static bool TryParseDate(string v, out DateOnly date) =>
            DateOnly.TryParse(v.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

        private static async Task<XLWorkbook> OpenWorkbookAsync(Stream file)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;
            return new XLWorkbook(ms);   // ClosedXML loads fully here, so ms can be disposed
        }
    }

    public class ExcelPreview
    {
        public List<string> Headers { get; set; } = new();
        public List<Dictionary<string, string>> SampleRows { get; set; } = new();
        public int TotalDataRows { get; set; }
    }

    public class ColumnMapping
    {
        public string SourceColumnName { get; set; } = "";

        // Empty means "don't import this column".
        public string TargetField { get; set; } = "";
    }
}

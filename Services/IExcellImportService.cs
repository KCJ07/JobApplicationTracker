namespace JobApplicationTracker.Services
{
    public interface IExcellImportService
    {
        Task<ExcelPreview> PreviewAsync(Stream file, int sampleRows = 10);

        Task<int> ImportAsync(Stream file, IReadOnlyList<ColumnMapping> mappings, string userId);
    }
}

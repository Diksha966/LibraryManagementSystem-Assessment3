namespace LibraryManagementSystem.Services.Exports;

public interface IExportService
{
    Task<byte[]> BuildLoansCsvAsync(CancellationToken cancellationToken = default);

    Task<byte[]> BuildNotificationsCsvAsync(CancellationToken cancellationToken = default);

    Task<byte[]> BuildDashboardPdfAsync(CancellationToken cancellationToken = default);
}

using LibraryManagementSystem.Services.Exports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Controllers.Api;

[ApiController]
[Route("api/exports")]
[Authorize(Roles = "Admin,Manager")]
public class ExportsApiController : ControllerBase
{
    private readonly IExportService _exportService;

    public ExportsApiController(IExportService exportService)
    {
        _exportService = exportService;
    }

    [HttpGet("loans.csv")]
    public async Task<IActionResult> GetLoansCsv(CancellationToken cancellationToken)
    {
        var content = await _exportService.BuildLoansCsvAsync(cancellationToken);

        return File(content, "text/csv", $"loans-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
    }

    [HttpGet("notifications.csv")]
    public async Task<IActionResult> GetNotificationsCsv(CancellationToken cancellationToken)
    {
        var content = await _exportService.BuildNotificationsCsvAsync(cancellationToken);

        return File(content, "text/csv", $"notifications-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
    }

    [HttpGet("dashboard.pdf")]
    public async Task<IActionResult> GetDashboardPdf(CancellationToken cancellationToken)
    {
        var content = await _exportService.BuildDashboardPdfAsync(cancellationToken);

        return File(content, "application/pdf", $"dashboard-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf");
    }
}

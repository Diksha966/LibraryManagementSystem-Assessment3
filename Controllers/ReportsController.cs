using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Controllers
{
    // Manager reports with on-screen preview and CSV / PDF export.
    [Authorize(Roles = "Manager")]
    public class ReportsController : Controller
    {
        private readonly ReportService _reports;

        public ReportsController(ReportService reports)
        {
            _reports = reports;
        }

        // GET: Reports
        public IActionResult Index()
        {
            return View();
        }

        // GET: Reports/Preview?type=borrowing
        public async Task<IActionResult> Preview(string? type)
        {
            var report = await _reports.BuildAsync(type);

            if (report == null)
                return NotFound();

            return View(report);
        }

        // GET: Reports/Download?type=fines&format=pdf
        public async Task<IActionResult> Download(string? type, string? format)
        {
            var report = await _reports.BuildAsync(type);

            if (report == null)
                return NotFound();

            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmm");
            string name = report.Key + "-report-" + stamp;

            if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
            {
                return File(
                    ReportExporter.ToPdf(report),
                    "application/pdf",
                    name + ".pdf");
            }

            if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            {
                return File(
                    ReportExporter.ToCsv(report),
                    "text/csv; charset=utf-8",
                    name + ".csv");
            }

            return BadRequest("Format must be csv or pdf.");
        }
    }
}

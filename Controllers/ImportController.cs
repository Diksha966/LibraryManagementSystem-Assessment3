using System.Text;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Controllers
{
    // Admin bulk import of catalogue items from a CSV file.
    [Authorize(Roles = "Admin")]
    public class ImportController : Controller
    {
        private const string SessionKey = "ImportCsvText";
        private const long MaxBytes = 1_000_000;

        private readonly CsvItemImportService _importer;

        public ImportController(CsvItemImportService importer)
        {
            _importer = importer;
        }

        // GET: Import
        public IActionResult Index()
        {
            return View();
        }

        // GET: Import/Template
        public IActionResult Template()
        {
            return File(
                Encoding.UTF8.GetBytes(CsvItemImportService.TemplateCsv),
                "text/csv",
                "library-items-template.csv");
        }

        // POST: Import/Preview
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Preview(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please choose a CSV file to upload.";
                return RedirectToAction(nameof(Index));
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Only .csv files can be imported.";
                return RedirectToAction(nameof(Index));
            }

            if (file.Length > MaxBytes)
            {
                TempData["Error"] = "The file is too large. The limit is 1 MB.";
                return RedirectToAction(nameof(Index));
            }

            string text;

            using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, true))
            {
                text = await reader.ReadToEndAsync();
            }

            HttpContext.Session.SetString(SessionKey, text);

            var preview = await _importer.ParseAsync(text);

            ViewBag.FileName = file.FileName;

            return View(preview);
        }

        // POST: Import/Confirm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm()
        {
            string? text = HttpContext.Session.GetString(SessionKey);

            if (string.IsNullOrEmpty(text))
            {
                TempData["Error"] = "Your upload has expired. Please upload the file again.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _importer.ImportAsync(text);

            HttpContext.Session.Remove(SessionKey);

            return View(result);
        }
    }
}

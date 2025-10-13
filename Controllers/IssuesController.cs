using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MunicipalityApp.Models;
using MunicipalityApp.Services; // IIssueStore
using System.Text;

namespace MunicipalityApp.Controllers;

public class IssuesController : Controller
{
    private readonly IIssueStore _store;
    private readonly IWebHostEnvironment _env;

    // ---- Whitelists ----
    private static readonly HashSet<string> AllowedExtensions = new(
        new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".heic" },
        StringComparer.OrdinalIgnoreCase
    );

    private static readonly HashSet<string> AllowedMime = new(
        new[]
        {
            "image/jpeg", "image/png", "image/gif",
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "image/heic", "image/heif"
        },
        StringComparer.OrdinalIgnoreCase
    );

    // ---- Limits ----
    private const long MaxEach = 10L * 1024L * 1024L; // 10 MB
    private const long MaxTotal = 20L * 1024L * 1024L; // 20 MB

    // ---- Categories for the form ----
    private static readonly string[] DefaultCategories =
        { "Sanitation", "Roads", "Water", "Electricity", "Refuse Collection", "Other" };

    public IssuesController(IIssueStore store, IWebHostEnvironment env)
    {
        _store = store;
        _env = env;
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.Categories = new[] { "Sanitation", "Roads", "Water", "Electricity", "Refuse Collection", "Other" };
        return View(new IssueInput());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 20_000_000)]
    public async Task<IActionResult> Create(IssueInput input, IFormFileCollection? files)
    {
        // Important: set categories again for redisplay on error
        ViewBag.Categories = new[] { "Sanitation", "Roads", "Water", "Electricity", "Refuse Collection", "Other" };
        if (!ModelState.IsValid) return View(input);

        var incoming = files ?? Request.Form.Files;
        long total = 0;

        // 1) Validate extensions, mime & sizes first
        foreach (var f in incoming)
        {
            if (f is null || f.Length <= 0) continue;

            var ext = Path.GetExtension(f.FileName) ?? "";
            if (!AllowedExtensions.Contains(ext))
            {
                ModelState.AddModelError(string.Empty, $"File type not allowed: {ext}");
            }

            total += f.Length;
            if (f.Length > MaxEach)
                ModelState.AddModelError(string.Empty, $"{Path.GetFileName(f.FileName)} exceeds 10 MB.");
            if (total > MaxTotal)
                ModelState.AddModelError(string.Empty, "Total upload exceeds 20 MB.");

            var contentType = f.ContentType ?? "application/octet-stream";
            // Allow HEIC even if browsers misreport; otherwise require an allowed MIME
            if (!AllowedMime.Contains(contentType) && !ext.Equals(".heic", StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError(string.Empty, $"{Path.GetFileName(f.FileName)} has an unsupported type ({contentType}).");
        }

        if (!ModelState.IsValid) return View(input);

        // 2) Create the Issue object
        var report = new IssueReport
        {
            Location = input.Location.Trim(),
            Category = input.Category.Trim(),
            Description = input.Description.Trim(),
            Status = IssueStatus.Submitted
        };

        // 3) Build a folder OUTSIDE wwwroot and save
        //    e.g., <contentroot>/AppUploads/issues/yyyy/MM/dd/
        var now = DateTime.UtcNow;
        var baseFolder = Path.Combine(_env.ContentRootPath, "AppUploads", "issues",
            now.ToString("yyyy"), now.ToString("MM"), now.ToString("dd"));
        Directory.CreateDirectory(baseFolder);

        foreach (var f in incoming)
        {
            if (f is null || f.Length <= 0) continue;

            var ext = (Path.GetExtension(f.FileName) ?? "").ToLowerInvariant();

            // Quick magic-byte sniff (best effort)
            if (!await QuickSniffValidAsync(f, ext))
            {
                ModelState.AddModelError(string.Empty, $"File '{Path.GetFileName(f.FileName)}' could not be validated.");
                return View(input);
            }

            // Store with GUID + extension only (don’t embed user filename)
            var storedName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(baseFolder, storedName);

            using (var stream = System.IO.File.Create(fullPath))
            {
                await f.CopyToAsync(stream);
            }

            var attach = new AttachmentRef
            {
                OriginalFileName = Path.GetFileName(f.FileName),
                StoredFilePath = fullPath,
                ContentType = f.ContentType ?? "application/octet-stream",
                SizeBytes = f.Length
            };

            report.Attachments.Enqueue(attach);
        }

        _store.Add(report);
        TempData["Success"] = "Thank you! Your report was submitted.";
        return RedirectToAction(nameof(Thanks), new { id = report.Id });
    }

    [HttpGet]
    public IActionResult Thanks(Guid id)
    {
        if (_store.TryGet(id, out var issue))
            return View(issue);

        TempData["Error"] = "We couldn't find that report.";
        return RedirectToAction(nameof(Create));
    }

    // ---- Safe download endpoint (serves from outside wwwroot) ----
    [HttpGet]
    public IActionResult Download(Guid id, Guid fileId)
    {
        if (!_store.TryGet(id, out var issue)) return NotFound();

        var att = issue.Attachments.FirstOrDefault(a => a.Id == fileId);
        if (att is null) return NotFound();

        if (!System.IO.File.Exists(att.StoredFilePath)) return NotFound();

        var stream = new FileStream(att.StoredFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var downloadName = string.IsNullOrWhiteSpace(att.OriginalFileName) ? "file" : att.OriginalFileName;
        var contentType = string.IsNullOrWhiteSpace(att.ContentType) ? "application/octet-stream" : att.ContentType;

        return File(stream, contentType, fileDownloadName: downloadName);
    }

    // ---- Light magic-byte checks ----
    private static async Task<bool> QuickSniffValidAsync(IFormFile file, string ext)
    {
        try
        {
            using var s = file.OpenReadStream();
            var header = new byte[8];
            var read = await s.ReadAsync(header, 0, header.Length);
            if (read <= 0) return false;

            // JPEG
            if ((ext == ".jpg" || ext == ".jpeg") && read >= 2)
                return header[0] == 0xFF && header[1] == 0xD8;

            // PNG
            if (ext == ".png" && read >= 8)
                return header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;

            // GIF
            if (ext == ".gif" && read >= 6)
                return header[0] == 'G' && header[1] == 'I' && header[2] == 'F';

            // PDF
            if (ext == ".pdf" && read >= 4)
                return header[0] == '%' && header[1] == 'P' && header[2] == 'D' && header[3] == 'F';

            // DOC/DOCX/HEIC — allow based on ext + MIME checks above
            if (ext == ".doc" || ext == ".docx" || ext == ".heic") return true;

            return true; // default allow for whitelisted ext handled above
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<string> GetCategories() => DefaultCategories;
}

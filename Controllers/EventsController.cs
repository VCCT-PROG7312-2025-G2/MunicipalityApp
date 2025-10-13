using Microsoft.AspNetCore.Mvc;
using MunicipalityApp.Services;
using MunicipalityApp.Models;

namespace MunicipalityApp.Controllers
{
    public class EventsController : Controller
    {
        private readonly IEventStore _events;
        private readonly ILogger<EventsController> _log;

        public EventsController(IEventStore events, ILogger<EventsController> log)
        {
            _events = events;
            _log = log;
        }

        // Increment 2: pagination added
        [HttpGet]
        public IActionResult Index(string? category, string? date, int page = 1, int size = 10)
        {
            _events.SeedIfEmpty();

            // Parse date (optional)
            DateOnly? parsedDate = null;
            if (!string.IsNullOrWhiteSpace(date) && DateOnly.TryParse(date, out var d))
                parsedDate = d;

            // Telemetry for recommendations
            _events.RecordSearch(category);

            var all = _events.Search(category, parsedDate).ToList();

            // clamp & paginate
            size = Math.Clamp(size, 5, 50);
            var total = all.Count;
            var pages = Math.Max(1, (int)Math.Ceiling(total / (double)size));
            page = Math.Clamp(page, 1, pages);
            var items = all.Skip((page - 1) * size).Take(size).ToList();

            ViewBag.Categories = _events.Categories;
            ViewBag.Recommendations = _events.GetRecommendations(3);
            ViewBag.Soonest = _events.GetSoonest(3);
            ViewBag.RecentlyViewed = _events.GetRecentlyViewed(5); // NEW

            // expose paging/filter in ViewBag for UI
            ViewBag.Page = page; ViewBag.Pages = pages; ViewBag.Size = size; ViewBag.Total = total;
            ViewBag.Category = category ?? ""; ViewBag.Date = date ?? "";

            _log.LogInformation("Events search category={Category} date={Date} page={Page} size={Size}",
                category, parsedDate?.ToString("yyyy-MM-dd"), page, size);

            return View(items);
        }

        [HttpGet]
        public IActionResult Details(Guid id)
        {
            var e = _events.GetById(id);
            if (e is null) return NotFound();

            // Increment 2: record view for Stack-backed "Recently viewed"
            _events.RecordViewed(e.Id);

            return View(e);
        }
    }
}

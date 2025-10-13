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

        [HttpGet]
        public IActionResult Index(string? category, string? date)
        {
            _events.SeedIfEmpty();

            // Parse date (optional)
            DateOnly? parsedDate = null;
            if (!string.IsNullOrWhiteSpace(date) && DateOnly.TryParse(date, out var d))
                parsedDate = d;

            // Telemetry for recommendations
            _events.RecordSearch(category);

            var results = _events.Search(category, parsedDate);
            ViewBag.Categories = _events.Categories;
            ViewBag.Recommendations = _events.GetRecommendations(3);
            ViewBag.Soonest = _events.GetSoonest(3);

            _log.LogInformation("Events search category={Category} date={Date}", category, parsedDate?.ToString("yyyy-MM-dd"));

            return View(results.ToList());
        }

        [HttpGet]
        public IActionResult Details(Guid id)
        {
            var e = _events.GetById(id);
            if (e is null) return NotFound();
            return View(e);
        }
    }
}

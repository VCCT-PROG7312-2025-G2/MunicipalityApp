using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MunicipalityApp.Models;
using MunicipalityApp.Services;

namespace MunicipalityApp.Controllers
{
    public class RequestsController : Controller
    {
        private readonly IIssueStore _store;
        private readonly ILogger<RequestsController> _log;

        public RequestsController(IIssueStore store, ILogger<RequestsController> log)
        {
            _store = store;
            _log = log;
        }

        [HttpGet]
        public IActionResult Index(
            string? q,
            string? status,
            string? category,
            string? from,
            string? to,
            string sort = "recent",
            int page = 1,
            int size = 10)
        {
            // base query
            var all = _store.All();

            // filters
            if (!string.IsNullOrWhiteSpace(q))
            {
                var needle = q.Trim();
                all = all.Where(i =>
                    (i.Location?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (i.Description?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                all = all.Where(i => string.Equals(i.Category, category, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<IssueStatus>(status, true, out var parsed))
            {
                all = all.Where(i => i.Status == parsed);
            }

            DateOnly? fromDate = null;
            DateOnly? toDate = null;
            if (!string.IsNullOrWhiteSpace(from) && DateOnly.TryParse(from, out var fd)) fromDate = fd;
            if (!string.IsNullOrWhiteSpace(to) && DateOnly.TryParse(to, out var td)) toDate = td;

            if (fromDate.HasValue)
                all = all.Where(i => DateOnly.FromDateTime(i.CreatedAt.ToLocalTime().Date) >= fromDate.Value);
            if (toDate.HasValue)
                all = all.Where(i => DateOnly.FromDateTime(i.CreatedAt.ToLocalTime().Date) <= toDate.Value);

            // sorting
            all = sort switch
            {
                "oldest" => all.OrderBy(i => i.CreatedAt),
                "status" => all.OrderBy(i => i.Status).ThenByDescending(i => i.CreatedAt),
                "category" => all.OrderBy(i => i.Category).ThenByDescending(i => i.CreatedAt),
                _ => all.OrderByDescending(i => i.CreatedAt) // recent
            };

            // snapshot
            var list = all.ToList();

            // paging
            size = Math.Clamp(size, 5, 50);
            var total = list.Count;
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)size));
            var curPage = Math.Clamp(page, 1, totalPages);
            var pageItems = list.Skip((curPage - 1) * size).Take(size).ToList();

            // data for filters
            ViewBag.Statuses = Enum.GetNames(typeof(IssueStatus)).ToList();
            ViewBag.Categories = _store.CategoryCounts.Keys.OrderBy(c => c).ToList();

            // surface state
            ViewBag.Query = q ?? "";
            ViewBag.Status = status ?? "";
            ViewBag.Category = category ?? "";
            ViewBag.From = from ?? "";
            ViewBag.To = to ?? "";
            ViewBag.Sort = sort;
            ViewBag.Page = curPage;
            ViewBag.Pages = totalPages;
            ViewBag.Size = size;
            ViewBag.Total = total;

            _log.LogInformation("Requests list q={q} status={status} category={category} from={from} to={to} sort={sort} page={page} size={size}",
                q, status, category, from, to, sort, curPage, size);

            return View(pageItems);
        }

        [HttpGet]
        public IActionResult Details(Guid id)
        {
            if (_store.TryGet(id, out var issue))
            {
                return View(issue);
            }
            return NotFound();
        }
    }
}

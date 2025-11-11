using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MunicipalityApp.Data;
using MunicipalityApp.Models;
using MunicipalityApp.Services;

namespace MunicipalityApp.Controllers
{
    public class RequestsController : Controller
    {
        private readonly IIssueStore _store;
        private readonly IRequestAnalytics _rx; // tree-backed analytics
        private readonly ILogger<RequestsController> _log;

        public RequestsController(IIssueStore store, IRequestAnalytics rx, ILogger<RequestsController> log)
        {
            _store = store;
            _rx = rx;
            _log = log;
        }

        [HttpGet]
        public IActionResult Index(
            string? q,
            string? status,
            string? category,
            string? from,
            string? to,
            string? locFrom,                 // BST lower bound (prefix-friendly)
            string? locTo,                   // BST upper bound (prefix-friendly)
            string sort = "recent",
            int page = 1,
            int size = 10)
        {
            // Ensure indexes are up-to-date before any query
            _rx.Rebuild();

            // ---- Parse dates (for AVL date index) ----
            DateOnly? fromDate = null;
            DateOnly? toDate = null;
            if (!string.IsNullOrWhiteSpace(from) && DateOnly.TryParse(from, out var fd)) fromDate = fd;
            if (!string.IsNullOrWhiteSpace(to) && DateOnly.TryParse(to, out var td)) toDate = td;

            // ---- Prefix-friendly BST bounds ----
            // If user supplies locTo="c", expand to "c\uFFFF" so all "c..." are included.
            string? locToForBst = locTo;
            if (!string.IsNullOrWhiteSpace(locTo))
                locToForBst = locTo + "\uFFFF";

            // Decide data source: BST (location range) if either bound provided; else AVL (date range)
            bool useBst = !string.IsNullOrWhiteSpace(locFrom) || !string.IsNullOrWhiteSpace(locTo);
            IEnumerable<IssueReport> all = useBst
                ? _rx.LocationsBetween(locFrom, locToForBst) // BST with prefix-friendly upper bound
                : _rx.RangeByDate(fromDate, toDate);         // AVL

            // ---- Text filter ----
            if (!string.IsNullOrWhiteSpace(q))
            {
                var needle = q.Trim();
                all = all.Where(i =>
                    (i.Location?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (i.Description?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            // ---- Category filter ----
            if (!string.IsNullOrWhiteSpace(category))
            {
                all = all.Where(i => string.Equals(i.Category, category, StringComparison.OrdinalIgnoreCase));
            }

            // ---- Status filter ----
            IssueStatus currentForPath = IssueStatus.Submitted;
            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<IssueStatus>(status, true, out var parsed))
            {
                all = all.Where(i => i.Status == parsed);
                currentForPath = parsed;
            }

            // ---- Sorting ----
            all = sort switch
            {
                "oldest" => all.OrderBy(i => i.CreatedAt),
                "status" => all.OrderBy(i => i.Status).ThenByDescending(i => i.CreatedAt),
                "category" => all.OrderBy(i => i.Category).ThenByDescending(i => i.CreatedAt),
                _ => all.OrderByDescending(i => i.CreatedAt) // "recent"
            };

            // ---- Snapshot + paging ----
            size = Math.Clamp(size, 5, 50);
            var list = all.ToList();
            var total = list.Count;
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)size));
            var curPage = Math.Clamp(page, 1, totalPages);
            var pageItems = list.Skip((curPage - 1) * size).Take(size).ToList();

            // ---- Data for filters ----
            ViewBag.Statuses = Enum.GetNames(typeof(IssueStatus)).ToList();
            ViewBag.Categories = _store.CategoryCounts.Keys.OrderBy(c => c).ToList();

            // ---- Panels from analytics ----
            ViewBag.TopUrgent = _rx.TopUrgent(5);
            ViewBag.WorkflowToResolved = _rx.WorkflowPath(currentForPath, IssueStatus.Resolved);
            ViewBag.DepartmentMst = _rx.DepartmentMst();

            // ---- Surface state ----
            ViewBag.Query = q ?? "";
            ViewBag.Status = status ?? "";
            ViewBag.Category = category ?? "";
            ViewBag.From = from ?? "";
            ViewBag.To = to ?? "";
            ViewBag.LocFrom = locFrom ?? "";
            ViewBag.LocTo = locTo ?? ""; // keep original text in the UI
            ViewBag.Sort = sort;
            ViewBag.Page = curPage;
            ViewBag.Pages = totalPages;
            ViewBag.Size = size;
            ViewBag.Total = total;

            _log.LogInformation(
                "Requests list q={q} status={status} category={category} from={from} to={to} locFrom={locFrom} locTo={locTo} (expandedTo={locToForBst}) sort={sort} page={page} size={size}",
                q, status, category, from, to, locFrom, locTo, locToForBst, sort, curPage, size);

            return View(pageItems);
        }

        [HttpGet]
        public IActionResult Details(Guid id)
        {
            // Prefer Red-Black Tree index (exact/ordered GUID lookup)
            var viaIndex = _rx.GetById(id);
            if (viaIndex.Count > 0)
            {
                _log.LogInformation("RBT lookup hit for {Id}", id);
                return View(viaIndex[0]);
            }

            // Fallback to the linked-list store traversal
            if (_store.TryGet(id, out var issue))
                return View(issue);

            return NotFound();
        }

        // NEW: Basic Tree demo – Requests → Status → Category → Issues
        [HttpGet]
        public IActionResult Hierarchy()
        {
            var root = _rx.BuildStatusCategoryTree();
            return View(root);
        }
    }
}

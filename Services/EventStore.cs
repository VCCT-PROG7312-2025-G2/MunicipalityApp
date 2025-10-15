using System;
using System.Collections.Generic;
using System.Linq;
using MunicipalityApp.Models;

namespace MunicipalityApp.Services
{
    public sealed class EventStore : IEventStore
    {
        // --- Data structures required by rubric ---
        private readonly SortedDictionary<DateOnly, List<EventItem>> _byDate = new(); // SortedDictionary
        private readonly HashSet<string> _categories = new(StringComparer.OrdinalIgnoreCase); // Set
        private readonly Queue<string> _recentSearches = new(); // Queue (search telemetry)
        private readonly Dictionary<string, int> _categoryHits = new(StringComparer.OrdinalIgnoreCase); // Dictionary
        private readonly PriorityQueue<EventItem, DateTime> _soonest = new(); // PriorityQueue
        private readonly Stack<Guid> _recentlyViewed = new(); // Stack (recent views)
        private readonly Dictionary<Guid, EventItem> _byId = new(); // fast lookup

        // NEW: co-occurrence learning (category association)
        private readonly Dictionary<string, Dictionary<string, int>> _coHits =
            new(StringComparer.OrdinalIgnoreCase);
        private string? _lastCategory;

        private readonly object _gate = new();

        public IReadOnlyCollection<string> Categories
        {
            get { lock (_gate) return _categories.OrderBy(c => c).ToList(); }
        }

        public void SeedIfEmpty()
        {
            lock (_gate)
            {
                if (_byDate.Count > 0) return;

                var today = DateOnly.FromDateTime(DateTime.Today);

                // ---- Seed at least 10 events (here: 12) ----
                Add(new EventItem { Title = "Ward 4 Cleanup", Category = "Community", Date = today.AddDays(3), Time = new(9, 0), Venue = "Hall A", Description = "Bring gloves and bags." });
                Add(new EventItem { Title = "Water Outage Briefing", Category = "Utilities", Date = today.AddDays(1), Time = new(18, 0), Venue = "Civic Centre", Description = "Planned maintenance briefing." });
                Add(new EventItem { Title = "Youth Sports Day", Category = "Sports", Date = today.AddDays(7), Time = new(10, 0), Venue = "Ward 2 Stadium" });
                Add(new EventItem { Title = "Recycling Workshop", Category = "Community", Date = today.AddDays(5), Time = new(14, 0), Venue = "Library Auditorium" });
                Add(new EventItem { Title = "Library Story Hour", Category = "Culture", Date = today.AddDays(2), Time = new(11, 0), Venue = "Central Library" });
                Add(new EventItem { Title = "Health Screening Pop-Up", Category = "Health", Date = today.AddDays(4), Time = new(8, 30), Venue = "Clinic B", Description = "Free BP & glucose checks." });
                Add(new EventItem { Title = "Fire Safety Talk", Category = "Safety", Date = today.AddDays(6), Time = new(17, 30), Venue = "Community Hall" });
                Add(new EventItem { Title = "Adult Education Open Day", Category = "Education", Date = today.AddDays(9), Time = new(16, 0), Venue = "College Annex" });
                Add(new EventItem { Title = "Small Business Expo", Category = "Business", Date = today.AddDays(12), Time = new(9, 30), Venue = "Expo Centre" });
                Add(new EventItem { Title = "Park Tree Planting", Category = "Environment", Date = today.AddDays(8), Time = new(8, 0), Venue = "Greenridge Park" });
                Add(new EventItem { Title = "Outdoor Concert", Category = "Arts", Date = today.AddDays(13), Time = new(19, 0), Venue = "Riverside Lawn" });
                Add(new EventItem { Title = "Tech Skills Bootcamp", Category = "Education", Date = today.AddDays(15), Time = new(9, 0), Venue = "Innovation Hub" });

                Add(new EventItem { Title = "Night Market", Category = "Business", Date = today.AddDays(10), Time = new(17, 0), Venue = "Old Town Square", Description = "Vendors, food trucks, live music." });
                Add(new EventItem { Title = "Safety Awareness Walk", Category = "Safety", Date = today.AddDays(11), Time = new(8, 0), Venue = "Ward 5 Police Station", Description = "Neighborhood safety initiative." });
                Add(new EventItem { Title = "Community Choir Gala", Category = "Arts", Date = today.AddDays(14), Time = new(18, 30), Venue = "Civic Theatre" });
                Add(new EventItem { Title = "Vaccination Drive", Category = "Health", Date = today.AddDays(16), Time = new(9, 0), Venue = "Clinic A", Description = "Flu and routine immunizations." });

            }
        }

        public IEnumerable<EventItem> Search(string? category, DateOnly? date)
        {
            lock (_gate)
            {
                IEnumerable<KeyValuePair<DateOnly, List<EventItem>>> dates = _byDate;
                if (date.HasValue) dates = dates.Where(kv => kv.Key == date.Value);

                var items = from kv in dates
                            from e in kv.Value
                            where string.IsNullOrWhiteSpace(category)
                                  || e.Category.Equals(category, StringComparison.OrdinalIgnoreCase)
                            orderby kv.Key, e.Time
                            select e;

                return items.ToList();
            }
        }

        public IReadOnlyList<string> GetRecommendations(int count)
        {
            lock (_gate)
            {
                return _categoryHits
                      .OrderByDescending(kv => kv.Value)
                      .Take(Math.Max(0, count))
                      .Select(kv => kv.Key)
                      .ToList();
            }
        }

        public IEnumerable<EventItem> GetSoonest(int count)
        {
            lock (_gate)
            {
                var tmp = new PriorityQueue<EventItem, DateTime>(_soonest.UnorderedItems);
                var list = new List<EventItem>(Math.Max(0, count));
                while (list.Count < count && tmp.TryDequeue(out var e, out _)) list.Add(e);
                return list;
            }
        }

        public IReadOnlyList<EventItem> GetRecentlyViewed(int count)
        {
            lock (_gate)
            {
                var seen = new HashSet<Guid>();
                var list = new List<EventItem>(Math.Max(0, count));
                foreach (var id in _recentlyViewed) // newest -> oldest
                {
                    if (seen.Add(id) && _byId.TryGetValue(id, out var e))
                    {
                        list.Add(e);
                        if (list.Count == count) break;
                    }
                }
                return list;
            }
        }

        public EventItem? GetById(Guid id)
        {
            lock (_gate) return _byId.TryGetValue(id, out var e) ? e : null;
        }

        public void RecordSearch(string? category)
        {
            if (string.IsNullOrWhiteSpace(category)) return;

            lock (_gate)
            {
                _recentSearches.Enqueue(category);
                _categoryHits[category] = _categoryHits.GetValueOrDefault(category) + 1;
                while (_recentSearches.Count > 40) _recentSearches.Dequeue();

                // Learn co-occurrence: last -> current and current -> last
                if (!string.IsNullOrWhiteSpace(_lastCategory) &&
                    !category.Equals(_lastCategory, StringComparison.OrdinalIgnoreCase))
                {
                    if (!_coHits.TryGetValue(_lastCategory!, out var a))
                        _coHits[_lastCategory!] = a = new(StringComparer.OrdinalIgnoreCase);
                    a[category] = a.GetValueOrDefault(category) + 1;

                    if (!_coHits.TryGetValue(category, out var b))
                        _coHits[category] = b = new(StringComparer.OrdinalIgnoreCase);
                    b[_lastCategory!] = b.GetValueOrDefault(_lastCategory!) + 1;
                }

                _lastCategory = category;
            }
        }

        public void RecordViewed(Guid id)
        {
            lock (_gate)
            {
                if (_byId.ContainsKey(id)) _recentlyViewed.Push(id);
                if (_recentlyViewed.Count > 200)
                {
                    var keep = new Stack<Guid>(_recentlyViewed.Take(100).Reverse());
                    _recentlyViewed.Clear();
                    foreach (var x in keep) _recentlyViewed.Push(x);
                }
            }
        }

        // --- New helpers for recommendation logic ---

        private IReadOnlyList<string> GetRelatedCategories(string seed, int count)
        {
            if (string.IsNullOrWhiteSpace(seed)) return Array.Empty<string>();

            if (!_coHits.TryGetValue(seed, out var inner) || inner.Count == 0)
                return Array.Empty<string>();

            return inner.OrderByDescending(kv => kv.Value)
                        .Take(Math.Max(0, count))
                        .Select(kv => kv.Key)
                        .ToList();
        }

        public IEnumerable<EventItem> GetRecommendedEvents(int count, string? seedCategory = null)
        {
            lock (_gate)
            {
                var wanted = new List<string>();

                if (!string.IsNullOrWhiteSpace(seedCategory))
                {
                    wanted.Add(seedCategory!);
                    wanted.AddRange(GetRelatedCategories(seedCategory!, 3));
                }

                // Backfill with global top categories by frequency if needed
                if (wanted.Count < 4)
                {
                    wanted.AddRange(
                        _categoryHits.OrderByDescending(kv => kv.Value)
                                     .Select(kv => kv.Key)
                                     .Where(c => !wanted.Contains(c, StringComparer.OrdinalIgnoreCase))
                                     .Take(4 - wanted.Count)
                    );
                }

                var unique = new HashSet<Guid>();
                var results = new List<EventItem>(Math.Max(0, count));

                // Choose the soonest upcoming items in desired categories
                foreach (var kv in _byDate) // SortedDictionary => chronological by date
                {
                    foreach (var e in kv.Value.OrderBy(x => x.Time))
                    {
                        if (wanted.Count == 0 ||
                            wanted.Exists(w => string.Equals(w, e.Category, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (unique.Add(e.Id))
                            {
                                results.Add(e);
                                if (results.Count == count) return results;
                            }
                        }
                    }
                }

                return results;
            }
        }

        // --- internals ---
        private void Add(EventItem e)
        {
            if (!_byDate.TryGetValue(e.Date, out var list)) _byDate[e.Date] = list = new();
            list.Add(e);
            _categories.Add(e.Category);
            _byId[e.Id] = e;

            var key = new DateTime(e.Date.Year, e.Date.Month, e.Date.Day, e.Time.Hour, e.Time.Minute, 0);
            _soonest.Enqueue(e, key);
        }
    }
}

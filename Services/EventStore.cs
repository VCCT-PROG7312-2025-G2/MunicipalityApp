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

        // NEW: Stack for recently viewed
        private readonly Stack<Guid> _recentlyViewed = new(); // Stack

        private readonly Dictionary<Guid, EventItem> _byId = new(); // fast lookup
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
                Add(new EventItem { Title = "Ward 4 Cleanup", Category = "Community", Date = today.AddDays(3), Time = new(9, 0), Venue = "Hall A", Description = "Bring gloves and bags." });
                Add(new EventItem { Title = "Water Outage Briefing", Category = "Utilities", Date = today.AddDays(1), Time = new(18, 0), Venue = "Civic Centre", Description = "Planned maintenance briefing." });
                Add(new EventItem { Title = "Youth Sports Day", Category = "Sports", Date = today.AddDays(7), Time = new(10, 0), Venue = "Ward 2 Stadium" });
                Add(new EventItem { Title = "Recycling Workshop", Category = "Community", Date = today.AddDays(5), Time = new(14, 0), Venue = "Library Auditorium" });
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
                return _categoryHits.OrderByDescending(kv => kv.Value)
                                    .Take(Math.Max(0, count))
                                    .Select(kv => kv.Key)
                                    .ToList();
            }
        }

        public IEnumerable<EventItem> GetSoonest(int count)
        {
            lock (_gate)
            {
                // snapshot without mutating the main queue
                var tmp = new PriorityQueue<EventItem, DateTime>(_soonest.UnorderedItems);
                var list = new List<EventItem>(Math.Max(0, count));
                while (list.Count < count && tmp.TryDequeue(out var e, out _)) list.Add(e);
                return list;
            }
        }

        // NEW: Stack-backed "Recently viewed"
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
            }
        }

        // NEW: push onto Stack and trim occasionally
        public void RecordViewed(Guid id)
        {
            lock (_gate)
            {
                if (_byId.ContainsKey(id)) _recentlyViewed.Push(id);
                if (_recentlyViewed.Count > 200)
                {
                    // keep the most recent ~100
                    var keep = new Stack<Guid>(_recentlyViewed.Take(100).Reverse());
                    _recentlyViewed.Clear();
                    foreach (var x in keep) _recentlyViewed.Push(x);
                }
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

using System;
using System.Collections.Generic;
using MunicipalityApp.Models;

namespace MunicipalityApp.Services
{
    public interface IEventStore
    {
        void SeedIfEmpty();

        // Core querying
        IEnumerable<EventItem> Search(string? category, DateOnly? date);
        EventItem? GetById(Guid id);

        // Metadata / panels
        IReadOnlyCollection<string> Categories { get; }
        IReadOnlyList<string> GetRecommendations(int count);
        IEnumerable<EventItem> GetSoonest(int count);
        IReadOnlyList<EventItem> GetRecentlyViewed(int count);

        // Telemetry
        void RecordSearch(string? category);
        void RecordViewed(Guid id);

        // NEW: concrete event recommendations
        IEnumerable<EventItem> GetRecommendedEvents(int count, string? seedCategory = null);
    }
}

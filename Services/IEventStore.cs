using System;
using System.Collections.Generic;
using MunicipalityApp.Models;

namespace MunicipalityApp.Services
{
    public interface IEventStore
    {
        void SeedIfEmpty();
        IEnumerable<EventItem> Search(string? category, DateOnly? date);
        IReadOnlyCollection<string> Categories { get; }
        IReadOnlyList<string> GetRecommendations(int count);
        IEnumerable<EventItem> GetSoonest(int count);

        // NEW in Increment 2
        IReadOnlyList<EventItem> GetRecentlyViewed(int count);
        void RecordViewed(Guid id);

        EventItem? GetById(Guid id);
        void RecordSearch(string? category);
    }
}

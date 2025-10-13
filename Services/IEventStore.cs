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
        EventItem? GetById(Guid id);
        void RecordSearch(string? category);
    }
}

using System;

namespace MunicipalityApp.Models
{
    public sealed class EventItem
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Title { get; init; } = "";
        public string Category { get; init; } = "";
        public DateOnly Date { get; init; }
        public TimeOnly Time { get; init; }
        public string Venue { get; init; } = "";
        public string? Description { get; init; }
    }
}

using MunicipalityApp.Models;
using MunicipalityApp.Services;

namespace MunicipalityApp.Infrastructure
{
    public static class DemoSeederExtensions
    {
        /// <summary>
        /// Seeds a realistic spread of issues across dates, statuses, and categories
        /// so AVL (date range), Heap (urgent), Graph (workflow), etc. are easy to demo.
        /// Seeds only when the store is empty.
        /// </summary>
        public static IApplicationBuilder SeedDemoIssues(this IApplicationBuilder app, bool enabled = true)
        {
            if (!enabled) return app;

            using var scope = app.ApplicationServices.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IIssueStore>();

            // Seed only when empty so user-submitted data isn't duplicated
            if (store.All().Any()) return app;

            var now = DateTime.UtcNow;

            void Add(string loc, string cat, string desc, DateTime whenUtc, IssueStatus st)
            {
                store.Add(new IssueReport
                {
                    Location = loc,
                    Category = cat,
                    Description = desc,
                    Status = st,
                    CreatedAt = whenUtc // init-only setter used in object initializer
                });
            }

            // A week spread (exercises AVL date range & urgency heap)
            Add("12 Market St, Ward 4", "Roads", "Pothole at intersection.", now.AddDays(-7), IssueStatus.Submitted);
            Add("Riverside Ave", "Electricity", "Streetlight not working.", now.AddDays(-6), IssueStatus.Assigned);
            Add("Clinic B", "Health", "Broken signage at entrance.", now.AddDays(-5), IssueStatus.InProgress);
            Add("Ward 2 Park", "Sanitation", "Overflowing bin near gate.", now.AddDays(-4), IssueStatus.Resolved);
            Add("Main Library", "Culture", "Noise complaint during study hours.", now.AddDays(-3), IssueStatus.Submitted);
            Add("Greenridge Park", "Environment", "Irrigation leak.", now.AddDays(-2), IssueStatus.Assigned);
            Add("Hall A", "Community", "Damaged door hinge.", now.AddDays(-1), IssueStatus.InProgress);
            Add("Water Plant", "Water", "Low pressure reported.", now, IssueStatus.Submitted);

            return app;
        }
    }
}

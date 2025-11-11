using MunicipalityApp.Infrastructure; // <-- add this
using MunicipalityApp.Services;

namespace MunicipalityApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // MVC
            builder.Services.AddControllersWithViews();

            // Core stores (in-memory)
            builder.Services.AddSingleton<IIssueStore, IssueStore>();
            builder.Services.AddSingleton<IEventStore, EventStore>();

            // Analytics/indexes (AVL, RBT, BST, BasicTree, Heap, Graph, MST)
            builder.Services.AddSingleton<IRequestAnalytics, RequestAnalytics>();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // --- DEMO SEEDER ---
            // Configurable: if Demo:SeedIssues is set, use that; otherwise seed in Development only.
            var seedEnabled = builder.Configuration.GetValue<bool?>("Demo:SeedIssues")
                               ?? app.Environment.IsDevelopment();
            app.SeedDemoIssues(seedEnabled);

            app.Run();
        }
    }
}


Author
Student: Steven Bomela / ST10304166
Module: PROG7312 – The IIE

MunicipalityApp — README
1. Introduction
MunicipalityApp(Nova) is a C# ASP.NET Core MVC application for PROG7312 – Task 2 (Implementation).
It enables residents to report municipal service issues and browse local events/announcements with simple recommendations.
This submission includes:
•	Part 1: Report Issues (fully implemented)
•	Part 2: Local Events & Announcements (implemented with data structures + recommendations)
•	Service Request Status: present as a page (for continuity/PoE), not required for Part 2 marks

2. Getting the code (Git)
Clone the Git repository  in vs 2022 or run the following on terminal:
git clone https://github.com/VCCT-PROG7312-2025-G2/MunicipalityApp.git
cd MunicipalityApp

3. Prerequisites
•	Windows 10/11
•	.NET 8.0 SDK
•	Visual Studio 2022 (Workloads: “.NET desktop development” and “ASP.NET and web development”)
Optional: You may also run via the .NET CLI (no Visual Studio required).

4. Build & Run
Option A — Visual Studio 2022
1.	Open MunicipalityApp.sln.
2.	Ensure target framework is .NET 8.0.
3.	Press F5 (Debug → Start Debugging).
4.	The app will launch at a https://localhost:xxxx/ URL shown in the output.


5. Application Usage
5.1 Main Menu
•	Three options:
o	Report Issues (active – Part 1)
o	Local Events & Announcements (active – Part 2)
o	Service Request Status (present for continuity; not required for Part 2)
•	Accessible navbar and card-based landing: Views/Shared/_Layout.cshtml and Views/Home/Index.cshtml.
5.2 Report Issues (Part 1)
Path: Issues → Create
1.	Provide:
o	Location (textbox)
o	Category (dropdown: Sanitation, Roads, Water, Electricity, Refuse Collection, Other)
o	Description (textarea, validated)
2.	Attach files (multi-file): .jpg, .jpeg, .png, .gif, .pdf, .doc, .docx, .heic
o	Limits: 10 MB per file, 20 MB total
o	Safe handling: extension & MIME whitelist, magic-byte sniff, anti-forgery token, size checks
o	Files are stored outside wwwroot under AppUploads/issues/yyyy/MM/dd/
3.	Engagement: progress bar + encouraging messages as you complete fields.
4.	Submit → Thanks page shows reference GUID, status badge/timeline, and download links for uploaded files.
5.3 Local Events & Announcements (Part 2)
Path: Events → Index
•	Filters: Category and Date (server-side)
•	Sorting: by date (default), name, or category via querystring ?sort=
•	Paging: page/size querystring, with accessible Previous/Next controls
•	Panels:
o	Recommended categories (based on search frequency & co-occurrence)
o	Recommended events (actual upcoming items inferred from your interest)
o	Coming up (soonest events via priority queue)
o	Recently viewed events
•	Event details page: title, date/time, venue, category, description + back navigation
Branding assets (already included):
wwwroot/images/city-background.jpg, wwwroot/images/city-of-nova-logo.png

6. Data Structures (what and where)
Part 1 (Issues) – no List/array for storage
•	LinkedList<IssueReport> — in-memory store of submitted issues (Services/IssueStore.cs)
•	Queue<AttachmentRef> — FIFO attachments per issue (Models/IssueReport.cs)
•	SortedDictionary<string,int> — counts per category (IssueStore.CategoryCounts)
Part 2 (Events & Announcements)
•	SortedDictionary<DateOnly, List<EventItem>> — primary calendar-ordered index (Services/EventStore.cs)
•	HashSet<string> — unique Categories (exposed to the UI)
•	Queue<string> — recent searches (telemetry for recommendations)
•	PriorityQueue<EventItem, DateTime> — soonest events (“Coming up” panel)
•	Stack<Guid> — recently viewed event IDs (LIFO)
•	Dictionary<Guid, EventItem> — fast byId lookup
•	Dictionary<string,int> — categoryHits (frequency of interest)
•	Dictionary<string, Dictionary<string,int>> — coHits for co-occurrence learning (related categories)
All of the above are thread-safe via a private lock (_gate) in EventStore.

7. Security & File Handling (Part 1 detail)
•	Whitelists: extensions (.jpg,.jpeg,.png,.gif,.pdf,.doc,.docx,.heic) + MIME types
•	Limits: MaxEach = 10 MB, MaxTotal = 20 MB
•	Magic-byte sniffing for common formats (JPEG/PNG/GIF/PDF)
•	Storage: outside wwwroot in AppUploads/... (prevents direct HTTP access)
•	Downloads: streamed through IssuesController.Download(id, fileId)
•	Anti-forgery on form posts

8. Project Structure (key files)

C:\Users\Bomel\source\repos\MunicipalityApp-fork\Controllers\EventsController.cs 
C:\Users\Bomel\source\repos\MunicipalityApp-fork\Controllers\HomeController.cs
C:\Users\Bomel\source\repos\MunicipalityApp-fork\Controllers\IssuesController.cs
C:\Users\Bomel\source\repos\MunicipalityApp-fork\Controllers\RequestsController.cs

C:\Users\Bomel\source\repos\MunicipalityApp-fork\Models\AttachmentRef.cs
C:\Users\Bomel\source\repos\MunicipalityApp-fork\Models\EventItem.cs
C:\Users\Bomel\source\repos\MunicipalityApp-fork\Models\IssueInput.cs
C:\Users\Bomel\source\repos\MunicipalityApp-fork\Models\IssueReport.cs

C:\Users\Bomel\source\repos\MunicipalityApp-fork\Services\EventStore.cs
C:\Users\Bomel\source\repos\MunicipalityApp-fork\Services\IEventStore.cs
C:\Users\Bomel\source\repos\MunicipalityApp-fork\Services\IIssueStore.cs
C:\Users\Bomel\source\repos\MunicipalityApp-fork\Services\IssueStore.cs

C:\Users\Bomel\source\repos\MunicipalityApp-fork\Views\Events\Details.cshtml
C:\Users\Bomel\source\repos\MunicipalityApp-fork\Views\Events\Index.cshtml

C:\Users\Bomel\source\repos\MunicipalityApp-fork\Views\Home\Index.cshtml
C:\Users\Bomel\source\repos\MunicipalityApp-fork\Views\Home\Privacy.cshtml

C:\Users\Bomel\source\repos\MunicipalityApp-fork\Views\Issues\Create.cshtml
C:\Users\Bomel\source\repos\MunicipalityApp-fork\Views\Issues\Thanks.cshtml

C:\Users\Bomel\source\repos\MunicipalityApp-fork\Views\Requests\Details.cshtml
C:\Users\Bomel\source\repos\MunicipalityApp-fork\Views\Requests\Index.cshtml

C:\Users\Bomel\source\repos\MunicipalityApp-fork\Views\Shared\_Layout.cshtml
C:\Users\Bomel\source\repos\MunicipalityApp-fork\Program.cs

9. Known Limitations
•	In-memory stores (events/issues) — data resets on restart.
•	Attachments saved to filesystem (AppUploads/...) and are not virus-scanned.
•	No authentication/authorization, notifications, or localization yet.
•	Events are seeded on first use of the Events page.

10. References / Resources
•	Microsoft Docs — ASP.NET Core MVC: https://learn.microsoft.com/aspnet/core/mvc
•	Microsoft Docs — File uploads in ASP.NET Core: https://learn.microsoft.com/aspnet/core/mvc/models/file-uploads
•	Microsoft Docs — Collections & Data Structures in .NET: https://learn.microsoft.com/dotnet/standard/collections
•	Bootstrap 5: https://getbootstrap.com

11. AI Usage (declaration)
I used ChatGPT (GPT ) to help structure the MVC solution, review data-structure choices (e.g., SortedDictionary, HashSet, Queue, PriorityQueue, Stack), and improve robustness of the file-upload flow (validation, size limits, magic-byte checks, anti-forgery). The generated suggestions were adapted and implemented by me, tested locally, and integrated into the final codebase. All domain logic and seeding data were designed to meet the PROG7312 Part 2 rubric, and I take responsibility for the final implementation and any changes made after AI assistance.





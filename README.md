MunicipalityApp — README
Student: Steven Bomela / ST10304166
Module: PROG7312 – The IIE
Framework: ASP.NET Core MVC (.NET 8.0)

1. Introduction
MunicipalityApp (Nova) is a C# ASP.NET Core MVC web application developed for PROG7312 – Task 3 (Final PoE).
It enables residents to:
•	Report service issues (e.g., potholes, leaks, power outages).
•	View local events & announcements (with recommendations).
•	Track the status of service requests, powered by advanced data structures for efficient retrieval and analytics.
This final submission includes:
•	Part 1: Report Issues (fully implemented)
•	Part 2: Local Events & Announcements (with data structures + recommendations)
•	Part 3: Service Request Status (advanced trees, heaps, graphs + MST)

2. Getting the Code (Git)
Clone the repository and open it in Visual Studio 2022 or .NET CLI:
git clone https://github.com/VCCT-PROG7312-2025-G2/MunicipalityApp.git
cd MunicipalityApp

3. Prerequisites
•	Windows 10/11 or macOS/Linux
•	.NET 8.0 SDK
•	Visual Studio 2022 with workloads:
o	“.NET desktop development”
o	“ASP.NET and web development”
Optional: You can also build/run using the .NET CLI.

4. Build & Run
Option A — Visual Studio 2022
1.	Open MunicipalityApp.sln.
2.	Confirm Target Framework = .NET 8.0.
3.	Press F5 → Start Debugging.
4.	The app launches at https://localhost:xxxx/.
Option B — . NET CLI
dotnet restore
dotnet build
dotnet run
Then open the printed URL (e.g. https://localhost:7100/) in your browser.

5. Application Usage
5.1 Main Menu
Three accessible tiles on the home screen:
•	Report Issues (active – Part 1)
•	Local Events & Announcements (active – Part 2)
•	Service Request Status (active – Part 3)
Navigation implemented in
Views/Shared/_Layout.cshtml and Views/Home/Index.cshtml.

5.2 Report Issues (Part 1)
Path: /Issues/Create
•	Fields: Location | Category | Description
•	File upload: .jpg . jpeg . png . gif . pdf . doc . docx . heic
•	Size limits: 10 MB per file, 20 MB total
•	Safe handling: extension & MIME whitelist + magic-byte sniffing + anti-forgery
•	Files stored outside wwwroot → AppUploads/issues/yyyy/MM/dd/
•	Progress bar + helpful messages enhance user engagement.
•	Upon submission: “Thank You” page shows Reference GUID + status timeline.

5.3 Local Events & Announcements (Part 2)
Path: /Events/Index
•	Filters: Category + Date
•	Sorting: by Date (default), Name, or Category
•	Pagination (5–50 per page)
•	Panels:
o	Recommended categories (based on search frequency + co-occurrence)
o	Recommended events (actual upcoming items)
o	Coming Up (PriorityQueue)
o	Recently Viewed (Stack)
•	Details page: Title | Date/Time | Venue | Category | Description
Branding assets:
wwwroot/images/city-background.jpg and wwwroot/images/city-of-nova-logo.png.

5.4 Service Request Status (Part 3)
Path: /Requests/Index
This module demonstrates advanced data structures and analytics for request tracking:
•	Filters:
o	Search (Location / Description)
o	Status (dropdown)
o	Category (dropdown)
o	Date range → AVL Tree
o	Location range → BST (prefix-friendly filter)
•	Sorting: Recent / Oldest / Status / Category
•	Pagination: page & size controls
•	Panels (right side):
o	Most Urgent → Heap (priority queue)
o	Workflow to Resolved → Graph + BFS
o	Dept Connectivity (MST) → Kruskal Minimum Spanning Tree
•	Hierarchy Tree View: Requests → Status → Category → Issues (Basic Tree)

6. Data Structures and Their Roles
Type	File(s)	Used For	Proof in UI
LinkedList	Services/IssueStore.cs	Issue storage	Submit and track issues
Queue	Models/IssueReport.cs	Attachment FIFO	Attachments per issue
SortedDictionary	Services/IssueStore.cs	Category counts	Status filters
SortedDictionary	Services/EventStore.cs	Events by date	Event listing
HashSet	EventStore.cs	Unique categories	Category filter
Stack	EventStore.cs	Recently viewed events	“Recently Viewed” panel
PriorityQueue	EventStore.cs / RequestAnalytics.cs	Soonest events / Urgency	“Coming Up” & “Most Urgent” panels
Dictionary	EventStore.cs	Hits & recommendations	Recommendations panel
BST (Binary Search Tree)	Data/Bst.cs	Location range search	Loc From/To fields
AVL Tree	Data/AvlTree.cs	Date range filter	From/To fields
Red-Black Tree	Data/RedBlackTree.cs	Id index (lookups)	Request Details
Heap	PriorityQueue<IssueReport,int>	Urgency ordering	“Most Urgent” panel
Graph + BFS	Data/Graph.cs	Workflow path → Resolved	“Workflow to Resolved” panel
MST (Kruskal)	Graph.KruskalMst()	Dept connectivity	“Dept Connectivity” panel
Basic Tree	Data/BasicTree.cs	Hierarchy (Status → Category → Issues)	/Requests/Hierarchy view
________________________________________
7. Security & File Handling
•	Whitelist: .jpg . jpeg . png . gif . pdf . doc . docx . heic
•	MIME checks + magic-byte sniffing
•	Anti-forgery tokens on form posts
•	Stored outside wwwroot for safety
•	Safe downloads via controller (streamed)

8. Project Structure (key files)
Controllers → IssuesController.cs, EventsController.cs, RequestsController.cs
Data → AvlTree.cs, Bst.cs, RedBlackTree.cs, Graph.cs, BasicTree.cs
Services → IssueStore.cs, EventStore.cs, RequestAnalytics.cs
Views → Issues/, Events/, Requests/, Shared/_Layout.cshtml
Infrastructure → DemoSeederExtensions.cs
Entry point → Program.cs

9. Known Limitations
•	In-memory stores (reset on restart).
•	No authentication or authorization.
•	File uploads not virus-scanned (just validated).
•	MST edges use simulated weights for demo purposes.

10. References / Resources
•	Microsoft Docs — ASP.NET Core MVC → https://learn.microsoft.com/aspnet/core/mvc
•	Microsoft Docs — File Uploads → https://learn.microsoft.com/aspnet/core/mvc/models/file-uploads
•	Microsoft Docs — Collections & Data Structures → https://learn.microsoft.com/dotnet/standard/collections
•	Bootstrap 5 → https://getbootstrap.com

11. AI Usage (Declaration)
I used ChatGPT (GPT-5) to help refine the MVC structure, analyze data-structure choices (e.g., AVL, RBT, BST, Heap, Graph, MST), and improve the robustness of the file-upload and analytics logic. All suggestions were adapted and tested by me, and I take full responsibility for the final implementation.

YouTube link: https://youtu.be/ZJf_I5WZUCw
Github link: https://github.com/VCCT-PROG7312-2025-G2/MunicipalityApp.git







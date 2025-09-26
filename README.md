MunicipalityApp – README 
1. Introduction 
MunicipalityApp is a C# ASP.NET Core MVC application developed for PROG7312 Task 2 
(Implementation). 
The purpose of the application is to facilitate citizen reporting of municipal service issues. 
The system provides a user-friendly interface for residents to submit issues, attach supporting 
f
 iles, and receive transparent feedback on their submissions. 
This version implements only the Report Issues functionality, with placeholders for future 
features (Local Events & Announcements and Service Request Status). 
2. Prerequisites 
To run this application, ensure the following are installed: 
• Visual Studio 2022 (with ASP.NET workload) 
• .NET 8.0 SDK 
• Windows operating system 
3. Build and Run Instructions 
1. Open the solution file MunicipalityApp.sln in Visual Studio 2022. 
2. Set the build target to .NET 8.0. 
3. Press F5 or select Debug → Start Debugging. 
4. The application will launch in a web browser at https://localhost:xxxx/. 
4. Application Usage 
4.1 Main Menu 
• On startup, the Main Menu is displayed. 
• Three options are presented: 
o Report Issues (enabled and functional) 
o Local Events & Announcements (disabled – future feature) 
o Service Request Status (disabled – future feature) 
4.2 Report Issues Workflow 
1. Select Report Issues. 
2. Complete the form by entering: 
o Location of the issue (textbox) 
o Category of the issue (dropdown list, e.g., Roads, Water, Sanitation) 
o Description of the issue (textarea / detailed input) 
3. Attach supporting files (images or documents). Accepted formats: 
o .jpg, .jpeg, .png, .gif, .pdf, .doc, .docx, .heic 
o Maximum file size: 10 MB each; 20 MB total per submission 
4. As the form is completed, the progress bar and encouraging messages guide the user. 
5. Submit the report using the Submit button. 
4.3 Confirmation Page 
After submission, users are redirected to a Thanks Page which provides: 
• A unique reference number (GUID) for the submission. 
• A status indicator (Submitted). 
• Badge and timeline progress bar for transparency of issue lifecycle. 
• Links to download any uploaded attachments. 
• Navigation options to submit another issue or return to the main menu. 
5. Data Structures 
To comply with the requirement of not using C# lists or arrays, the following data structures 
were used: 
• LinkedList<IssueReport> → stores all reported issues (efficient insertions). 
• Queue<AttachmentRef> → stores attachments for each issue in FIFO order. 
• SortedDictionary<string,int> → tracks counts of issues per category. 
This approach demonstrates practical understanding of alternative data structures while 
meeting the assignment brief. 
6. Features Implemented 
• Main Menu with three tasks (only Report Issues active). 
• Report Issues form with validation, file uploads, and progress bar. 
• Encouraging messages as part of the engagement strategy. 
• Thanks Page with reference number, attachments, and transparency features. 
• Anti-forgery protection and secure file handling. 
• Responsive design with Bootstrap and custom CSS theme. 
7. Design Considerations 
• Consistency: Unified colour scheme and card-based layout. 
• Clarity: Clear labels, input placeholders, and error messages. 
• User Feedback: Alerts, validation summaries, success messages, and status 
indicators. 
• Responsiveness: Bootstrap grid ensures compatibility with multiple screen sizes. 
8. Project Structure 
MunicipalityApp/ 
├Controllers/ 
│   ├ HomeController.cs 
│    
├IssuesController.cs 
├Models/ 
│   ├ IssueInput.cs 
│   ├ IssueReport.cs 
│   ├ AttachmentRef.cs 
│   ├ IssueStatus.cs 
├ Services/ 
│   ├ IIssueStore.cs 
│   ├ IssueStore.cs 
├Views/ 
│   ├Home/Index.cshtml 
│   ├ Issues/Create.cshtml 
│   ├ Issues/Thanks.cshtml 
│   ├Shared/_Layout.cshtml 
├ wwwroot/ 
│   ├css/site.css 
│   └── uploads/ 
├ Program.cs 
9. Known Limitations 
• Issues are stored in-memory and are lost when the application restarts. 
• Attachments are saved to the filesystem under wwwroot/uploads/…. 
• Database integration, notifications (SMS/email), and multi-language support are not yet 
implemented. 
10. Rubric Compliance 
• Task presentation on startup → Three tasks shown, two disabled, one active. 
• Report Issues → All required inputs, file upload, submit, navigation. 
• User input for issue details → Validation implemented, error messages displayed. 
• Media attachment functionality → Multi-file upload with validation and persistence. 
• User engagement strategy → Progress bar + encouraging labels + status 
badge/timeline. 
• Data structures → LinkedList, Queue, SortedDictionary (no List/array). 
• Event handling → Fully implemented in MVC controllers with validation and feedback. 
• Design considerations → Consistent, clear, responsive UI with user feedback. 
• Readme file quality → This document. 
11. Author 
Student: [Steven Bomela / ST10304166] 
Module: PROG7312 
Institution: The IIE 
12. References / Resources 
• Microsoft Docs. (2024). ASP.NET Core MVC documentation. Available at: 
https://learn.microsoft.com/aspnet/core/mvc 
• Microsoft Docs. (2024). Working with files in ASP.NET Core. Available at: 
https://learn.microsoft.com/aspnet/core/mvc/models/file-uploads 
• Microsoft Docs. (2024). Collections and Data Structures in C#. Available at: 
https://learn.microsoft.com/dotnet/standard/collections 
• Bootstrap. (2024). Bootstrap 5 Documentation. Available at: https://getbootstrap.com

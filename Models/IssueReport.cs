using System.Collections.Generic;

namespace MunicipalityApp.Models
{
    public enum IssueStatus { Submitted, Assigned, InProgress, Resolved }

    public sealed class IssueReport
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        public string Location { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";

        // FIFO queue is fine for Part 1
        public Queue<AttachmentRef> Attachments { get; } = new();

        public IssueStatus Status { get; set; } = IssueStatus.Submitted;
    }
}

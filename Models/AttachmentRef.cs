namespace MunicipalityApp.Models
{
    public sealed class AttachmentRef
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        // Original client filename (for download name only)
        public string OriginalFileName { get; init; } = "";

        // Full server path OUTSIDE wwwroot
        public string StoredFilePath { get; init; } = "";

        public string ContentType { get; init; } = "application/octet-stream";
        public long SizeBytes { get; init; }
    }
}

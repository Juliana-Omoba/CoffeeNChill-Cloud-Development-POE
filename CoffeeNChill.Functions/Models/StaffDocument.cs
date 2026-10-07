namespace CoffeeNChill.Functions.Models
{
    public class StaffDocument
    {
        // Name of the uploaded file
        public string FileName { get; set; } = string.Empty;

        // File extension (.pdf, .docx, etc.)
        public string FileExtension { get; set; } = string.Empty;

        // MIME type
        public string ContentType { get; set; } = string.Empty;

        // File size in bytes
        public long FileSize { get; set; }

        // Date and time uploaded
        public DateTime UploadedOn { get; set; }

        // Azure Blob Storage container name
        public string ContainerName { get; set; } = "staff-docs";
    }
}
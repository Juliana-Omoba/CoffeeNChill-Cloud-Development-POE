namespace CoffeeNChill.Functions.DTOs.StaffDocuments


{
    public class StaffDocumentResponse
    {
        // Name of the uploaded document
        public string FileName { get; set; } = string.Empty;

        // File extension, for example .pdf or .docx
        public string FileExtension { get; set; } = string.Empty;

        // MIME type, for example application/pdf
        public string ContentType { get; set; } = string.Empty;

        // File size in bytes
        public long FileSize { get; set; }

        // Date and time the document was uploaded
        public DateTime UploadedOn { get; set; }

        // Azure Blob Storage container
        public string ContainerName { get; set; } = string.Empty;
    }
}
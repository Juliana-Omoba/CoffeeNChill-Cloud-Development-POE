using CoffeeNChill.Functions.DTOs.StaffDocuments;
using CoffeeNChill.Functions.Interfaces;
using CoffeeNChill.Functions.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net;

namespace CoffeeNChill.Functions.Functions.StaffDocuments
{
    public class UploadStaffDocumentFunction
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<UploadStaffDocumentFunction> _logger;

        // --------------------------------------------------
        // Maximum allowed upload size: 10 MB
        // --------------------------------------------------
        private const long MaxFileSize =
            10 * 1024 * 1024;

        // --------------------------------------------------
        // Allowed file extensions and their expected
        // MIME types
        // --------------------------------------------------
        private static readonly Dictionary<string, string>
            AllowedFileTypes =
                new(StringComparer.OrdinalIgnoreCase)
                {
                    {
                        ".pdf",
                        "application/pdf"
                    },
                    {
                        ".doc",
                        "application/msword"
                    },
                    {
                        ".docx",
                        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                    }
                };

        // --------------------------------------------------
        // Constructor / Dependency Injection
        // --------------------------------------------------
        public UploadStaffDocumentFunction(
            IFileStorageService fileStorageService,
            ILogger<UploadStaffDocumentFunction> logger)
        {
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        // --------------------------------------------------
        // HTTP POST Function:
        // POST /api/documents/upload
        // --------------------------------------------------
        [Function("UploadStaffDocument")]
        public async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "post",
                Route = "documents/upload")]
            HttpRequest req)
        {
            _logger.LogInformation(
                "Staff document upload request received.");

            try
            {
                // --------------------------------------------------
                // 1. Validate request content type
                // --------------------------------------------------
                if (!req.HasFormContentType)
                {
                    _logger.LogWarning(
                        "Upload rejected because multipart/form-data was not used.");

                    return new BadRequestObjectResult(new
                    {
                        error =
                            "The request must use multipart/form-data."
                    });
                }

                // --------------------------------------------------
                // 2. Read the multipart form
                // --------------------------------------------------
                var form =
                    await req.ReadFormAsync();

                IFormFile? file =
                    form.Files["file"];

                // --------------------------------------------------
                // 3. Validate that a file was supplied
                // --------------------------------------------------
                if (file == null)
                {
                    _logger.LogWarning(
                        "Upload rejected because no file was supplied.");

                    return new BadRequestObjectResult(new
                    {
                        error =
                            "Please upload a file using the form-data field named 'file'."
                    });
                }

                // --------------------------------------------------
                // 4. Validate that the file is not empty
                // --------------------------------------------------
                if (file.Length == 0)
                {
                    _logger.LogWarning(
                        "Upload rejected because the file was empty.");

                    return new BadRequestObjectResult(new
                    {
                        error =
                            "The uploaded file is empty."
                    });
                }

                // --------------------------------------------------
                // 5. Validate maximum file size
                // --------------------------------------------------
                if (file.Length > MaxFileSize)
                {
                    _logger.LogWarning(
                        "Upload rejected because file {FileName} exceeded 10 MB.",
                        file.FileName);

                    return new BadRequestObjectResult(new
                    {
                        error =
                            "The uploaded file exceeds the maximum allowed size of 10 MB."
                    });
                }

                // --------------------------------------------------
                // 6. Validate file name
                // --------------------------------------------------
                if (string.IsNullOrWhiteSpace(
                    file.FileName))
                {
                    _logger.LogWarning(
                        "Upload rejected because the file name was invalid.");

                    return new BadRequestObjectResult(new
                    {
                        error =
                            "The uploaded file must have a valid file name."
                    });
                }

                // --------------------------------------------------
                // 7. Obtain and validate file extension
                // --------------------------------------------------
                string extension =
                    Path.GetExtension(file.FileName)
                        .ToLowerInvariant();

                if (!AllowedFileTypes.ContainsKey(
                    extension))
                {
                    _logger.LogWarning(
                        "Upload rejected because file extension {Extension} is not allowed.",
                        extension);

                    return new BadRequestObjectResult(new
                    {
                        error =
                            "Invalid file type. Only PDF, DOC and DOCX files are allowed."
                    });
                }

                // --------------------------------------------------
                // 8. Validate MIME type
                // --------------------------------------------------
                string expectedMimeType =
                    AllowedFileTypes[extension];

                if (!string.Equals(
                    file.ContentType,
                    expectedMimeType,
                    StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "Upload rejected because MIME type {MimeType} does not match extension {Extension}.",
                        file.ContentType,
                        extension);

                    return new BadRequestObjectResult(new
                    {
                        error =
                            $"Invalid MIME type. A {extension} file must use content type '{expectedMimeType}'."
                    });
                }

                // --------------------------------------------------
                // 9. Upload document to Blob Storage
                // --------------------------------------------------
                StaffDocument document =
                    await _fileStorageService
                        .UploadDocumentAsync(file);

                // --------------------------------------------------
                // 10. Convert Model to Response DTO
                // --------------------------------------------------
                var responseDto =
                    new StaffDocumentResponse
                    {
                        FileName =
                            document.FileName,

                        FileExtension =
                            document.FileExtension,

                        ContentType =
                            document.ContentType,

                        FileSize =
                            document.FileSize,

                        UploadedOn =
                            document.UploadedOn,

                        ContainerName =
                            document.ContainerName
                    };

                // --------------------------------------------------
                // 11. Log successful upload
                // --------------------------------------------------
                _logger.LogInformation(
                    "Staff document {FileName} uploaded successfully. Size: {FileSize} bytes.",
                    document.FileName,
                    document.FileSize);

                // --------------------------------------------------
                // 12. Return successful response
                // --------------------------------------------------
                return new OkObjectResult(
                    responseDto);
            }
            catch (Exception ex)
            {
                // --------------------------------------------------
                // 13. Log and handle unexpected errors
                // --------------------------------------------------
                _logger.LogError(
                    ex,
                    "Unexpected error while uploading staff document.");

                return new ObjectResult(new
                {
                    error =
                        "An unexpected error occurred while uploading the document."
                })
                {
                    StatusCode =
                        (int)HttpStatusCode
                            .InternalServerError
                };
            }
        }
    }
}
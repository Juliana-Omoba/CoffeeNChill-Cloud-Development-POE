using CoffeeNChill.Functions.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace CoffeeNChill.Functions.Functions.StaffDocuments
{
    public class DownloadStaffDocumentFunction
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<DownloadStaffDocumentFunction> _logger;

        public DownloadStaffDocumentFunction(
            IFileStorageService fileStorageService,
            ILogger<DownloadStaffDocumentFunction> logger)
        {
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        [Function("DownloadStaffDocument")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "documents/download/{fileName}")]
            HttpRequestData req,
            string fileName)
        {
            _logger.LogInformation(
                "Retrieving staff document: {FileName}",
                fileName);

            try
            {
                // --------------------------------------------------
                // 1. Validate file name
                // --------------------------------------------------

                if (string.IsNullOrWhiteSpace(fileName))
                {
                    var badRequest =
                        req.CreateResponse(
                            HttpStatusCode.BadRequest);

                    await badRequest.WriteAsJsonAsync(new
                    {
                        message = "A file name is required."
                    });

                    return badRequest;
                }

                // --------------------------------------------------
                // 2. Retrieve document from Blob Storage
                // --------------------------------------------------

                Stream? fileStream =
                    await _fileStorageService
                        .DownloadDocumentAsync(fileName);

                // --------------------------------------------------
                // 3. Document not found
                // --------------------------------------------------

                if (fileStream == null)
                {
                    _logger.LogWarning(
                        "Staff document not found: {FileName}",
                        fileName);

                    var notFound =
                        req.CreateResponse(
                            HttpStatusCode.NotFound);

                    await notFound.WriteAsJsonAsync(new
                    {
                        message =
                            "The requested document was not found."
                    });

                    return notFound;
                }

                // --------------------------------------------------
                // 4. Create successful response
                // --------------------------------------------------

                var response =
                    req.CreateResponse(
                        HttpStatusCode.OK);

                response.Headers.Add(
                    "Content-Type",
                    GetContentType(fileName));

                response.Headers.Add(
                    "Content-Disposition",
                    $"attachment; filename=\"{fileName}\"");

                // --------------------------------------------------
                // 5. Send document to client
                // --------------------------------------------------

                await fileStream.CopyToAsync(
                    response.Body);

                await fileStream.DisposeAsync();

                _logger.LogInformation(
                    "Staff document retrieved successfully: {FileName}",
                    fileName);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving staff document: {FileName}",
                    fileName);

                var errorResponse =
                    req.CreateResponse(
                        HttpStatusCode.InternalServerError);

                await errorResponse.WriteAsJsonAsync(new
                {
                    message =
                        "An unexpected error occurred while retrieving the document."
                });

                return errorResponse;
            }
        }

        private static string GetContentType(
            string fileName)
        {
            string extension =
                Path.GetExtension(fileName)
                    .ToLowerInvariant();

            return extension switch
            {
                ".pdf" =>
                    "application/pdf",

                ".doc" =>
                    "application/msword",

                ".docx" =>
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",

                ".xls" =>
                    "application/vnd.ms-excel",

                ".xlsx" =>
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                ".ppt" =>
                    "application/vnd.ms-powerpoint",

                ".pptx" =>
                    "application/vnd.openxmlformats-officedocument.presentationml.presentation",

                ".txt" =>
                    "text/plain",

                ".csv" =>
                    "text/csv",

                ".jpg" =>
                    "image/jpeg",

                ".jpeg" =>
                    "image/jpeg",

                ".png" =>
                    "image/png",

                _ =>
                    "application/octet-stream"
            };
        }
    }
}
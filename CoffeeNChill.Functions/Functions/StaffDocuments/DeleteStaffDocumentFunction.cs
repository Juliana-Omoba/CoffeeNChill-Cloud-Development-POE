using CoffeeNChill.Functions.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace CoffeeNChill.Functions.Functions.StaffDocuments
{
    public class DeleteStaffDocumentFunction
    {
        private readonly IFileStorageService
            _fileStorageService;

        private readonly ILogger<DeleteStaffDocumentFunction>
            _logger;

        // --------------------------------------------------
        // Constructor / Dependency Injection
        // --------------------------------------------------
        public DeleteStaffDocumentFunction(
            IFileStorageService fileStorageService,
            ILogger<DeleteStaffDocumentFunction> logger)
        {
            _fileStorageService =
                fileStorageService;

            _logger =
                logger;
        }

        // --------------------------------------------------
        // HTTP DELETE Function:
        // DELETE /api/documents/{fileName}
        // --------------------------------------------------
        [Function("DeleteStaffDocument")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "delete",
                Route = "documents/{fileName}")]
            HttpRequestData req,
            string fileName)
        {
            _logger.LogInformation(
                "Deleting staff document: {FileName}",
                fileName);

            try
            {
                // --------------------------------------------------
                // 1. Validate file name
                // --------------------------------------------------
                if (string.IsNullOrWhiteSpace(
                    fileName))
                {
                    var badRequest =
                        req.CreateResponse(
                            HttpStatusCode.BadRequest);

                    await badRequest
                        .WriteAsJsonAsync(new
                        {
                            error =
                                "A file name is required."
                        });

                    return badRequest;
                }

                // --------------------------------------------------
                // 2. Attempt to delete document
                // --------------------------------------------------
                bool deleted =
                    await _fileStorageService
                        .DeleteDocumentAsync(
                            fileName);

                // --------------------------------------------------
                // 3. Handle document not found
                // --------------------------------------------------
                if (!deleted)
                {
                    _logger.LogWarning(
                        "Staff document was not found for deletion: {FileName}",
                        fileName);

                    var notFound =
                        req.CreateResponse(
                            HttpStatusCode.NotFound);

                    await notFound
                        .WriteAsJsonAsync(new
                        {
                            error =
                                $"Document '{fileName}' was not found."
                        });

                    return notFound;
                }

                // --------------------------------------------------
                // 4. Log successful deletion
                // --------------------------------------------------
                _logger.LogInformation(
                    "Staff document deleted successfully: {FileName}",
                    fileName);

                // --------------------------------------------------
                // 5. Return successful response
                // --------------------------------------------------
                var response =
                    req.CreateResponse(
                        HttpStatusCode.OK);

                await response
                    .WriteAsJsonAsync(new
                    {
                        message =
                            $"Document '{fileName}' was deleted successfully."
                    });

                return response;
            }
            catch (Exception ex)
            {
                // --------------------------------------------------
                // 6. Log and handle unexpected errors
                // --------------------------------------------------
                _logger.LogError(
                    ex,
                    "Error deleting staff document: {FileName}",
                    fileName);

                var errorResponse =
                    req.CreateResponse(
                        HttpStatusCode
                            .InternalServerError);

                await errorResponse
                    .WriteAsJsonAsync(new
                    {
                        error =
                            "An unexpected error occurred while deleting the document."
                    });

                return errorResponse;
            }
        }
    }
}
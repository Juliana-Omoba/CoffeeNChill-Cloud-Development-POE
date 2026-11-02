
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;

namespace CoffeeNChill.ApiGateway.Controllers
{
    // =====================================================
    // POE PART 3: STAFF DOCUMENT API GATEWAY CONTROLLER
    // =====================================================
    //
    // PURPOSE:
    //
    // Provide secure API endpoints for Staff and Admin
    // users to manage documents stored in Azure Blob
    // Storage through the existing Azure Functions.
    //
    // This controller supports:
    //
    // 1. Uploading staff documents.
    // 2. Listing staff documents.
    // 3. Downloading staff documents.
    // 4. Deleting staff documents.
    //
    // ARCHITECTURE:
    //
    // Swagger / API Client
    //         |
    //         v
    // API Gateway (this controller)
    //         |
    //         v
    // Azure Functions
    //         |
    //         v
    // Azure Blob Storage
    //
    // IMPORTANT:
    //
    // Existing Azure Functions are preserved.
    //
    // Authentication and role-based authorisation
    // are enforced by the API Gateway.
    //
    // Azure Functions must also be protected against
    // unauthorised direct access before deployment.
    // =====================================================

    [ApiController]
    [Route("api/documents")]
    [Authorize(Roles = "Staff,Admin")]
    public class DocumentsController : ControllerBase
    {
        // =================================================
        // STEP 1: DEPENDENCIES
        // =================================================
        //
        // IHttpClientFactory creates the existing named
        // HTTP client registered in Program.cs.
        //
        // ILogger records operational failures.
        // =================================================

        private readonly IHttpClientFactory
            _httpClientFactory;

        private readonly ILogger<DocumentsController>
            _logger;

        // Maximum supported document size: 10 MB.
        //
        // This matches UploadStaffDocumentFunction.
        private const long MaxFileSize =
            10 * 1024 * 1024;

        // Maximum multipart request size includes some
        // additional space for form-data boundaries.
        private const long MaxRequestSize =
            MaxFileSize + (1024 * 1024);

        // Allowed extensions match the existing
        // Azure Functions upload validation.
        private static readonly HashSet<string>
            AllowedExtensions =
                new(StringComparer.OrdinalIgnoreCase)
                {
                    ".pdf",
                    ".doc",
                    ".docx"
                };

        // =================================================
        // STEP 2: CONSTRUCTOR
        // =================================================

        public DocumentsController(
            IHttpClientFactory httpClientFactory,
            ILogger<DocumentsController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // =================================================
        // STEP 3: UPLOAD STAFF DOCUMENT
        // =================================================
        //
        // Gateway:
        // POST /api/documents/upload
        //
        // Azure Function:
        // POST /api/documents/upload
        //
        // The request must use multipart/form-data.
        //
        // The form field must be named "file".
        //
        // Supported extensions:
        // PDF, DOC, DOCX.
        //
        // Maximum file size:
        // 10 MB.
        // =================================================

        /// <summary>
        /// Uploads a PDF, DOC or DOCX staff document.
        /// Requires Staff or Admin.
        /// </summary>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxRequestSize)]
        [RequestFormLimits(
            MultipartBodyLengthLimit = MaxRequestSize)]
        public async Task<IActionResult> Upload(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            // Validate that a file was supplied.
            if (file == null)
            {
                return BadRequest(new
                {
                    error =
                        "Please select a document to upload."
                });
            }

            // Reject empty documents.
            if (file.Length == 0)
            {
                return BadRequest(new
                {
                    error =
                        "The uploaded document is empty."
                });
            }

            // Enforce the same 10 MB limit as Functions.
            if (file.Length > MaxFileSize)
            {
                return BadRequest(new
                {
                    error =
                        "The maximum allowed file size is 10 MB."
                });
            }

            // Validate the original filename.
            if (string.IsNullOrWhiteSpace(file.FileName))
            {
                return BadRequest(new
                {
                    error =
                        "The document must have a valid filename."
                });
            }

            // Validate the file extension.
            string extension =
                Path.GetExtension(file.FileName);

            if (!AllowedExtensions.Contains(extension))
            {
                return BadRequest(new
                {
                    error =
                        "Only PDF, DOC and DOCX documents are allowed."
                });
            }

            try
            {
                // Create the configured Azure Functions
                // HTTP client.
                var client =
                    _httpClientFactory.CreateClient(
                        "AzureFunctions");

                // Prepare a multipart/form-data request.
                //
                // This is essential because the Azure
                // Function reads:
                //
                // form.Files["file"]
                using var formContent =
                    new MultipartFormDataContent();

                // Open the uploaded file as a stream.
                //
                // The stream remains available until
                // the backend request completes.
                using var inputStream =
                    file.OpenReadStream();

                using var fileContent =
                    new StreamContent(inputStream);

                // Preserve the content type supplied
                // by the uploaded file.
                //
                // The Azure Function will independently
                // validate the MIME type.
                fileContent.Headers.ContentType =
                    new MediaTypeHeaderValue(
                        string.IsNullOrWhiteSpace(
                            file.ContentType)
                            ? "application/octet-stream"
                            : file.ContentType);

                // Add the file using the required
                // form-data field name "file".
                formContent.Add(
                    fileContent,
                    "file",
                    Path.GetFileName(file.FileName));

                // Forward the multipart request.
                using var request =
                    new HttpRequestMessage(
                        HttpMethod.Post,
                        "api/documents/upload");

                request.Content = formContent;

                using var response =
                    await client.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                // Preserve the Azure Function response.
                return await ForwardResponseAsync(
                    response,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Staff document upload timed out.");

                return StatusCode(
                    StatusCodes.Status504GatewayTimeout,
                    new
                    {
                        error =
                            "The document upload service did not respond in time."
                    });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to reach Azure Functions during document upload.");

                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        error =
                            "The API Gateway could not reach Azure Functions."
                    });
            }
        }

        // =================================================
        // STEP 4: LIST STAFF DOCUMENTS
        // =================================================
        //
        // Gateway:
        // GET /api/documents
        //
        // Azure Function:
        // GET /api/documents
        //
        // Returns the document metadata as JSON.
        // =================================================

        /// <summary>
        /// Retrieves all staff document metadata.
        /// Requires Staff or Admin.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken)
        {
            return await ForwardJsonRequestAsync(
                HttpMethod.Get,
                "api/documents",
                cancellationToken);
        }

        // =================================================
        // STEP 5: DOWNLOAD STAFF DOCUMENT
        // =================================================
        //
        // Gateway:
        // GET /api/documents/download/{fileName}
        //
        // Azure Function:
        // GET /api/documents/download/{fileName}
        //
        // IMPORTANT:
        //
        // Successful downloads return actual file bytes.
        //
        // Do not use ContentResult for a PDF or DOCX.
        // =================================================

        /// <summary>
        /// Downloads a staff document from Azure Blob Storage.
        /// Requires Staff or Admin.
        /// </summary>
        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> Download(
            string fileName,
            CancellationToken cancellationToken)
        {
            if (!IsValidFileName(fileName))
            {
                return BadRequest(new
                {
                    error = "Invalid document filename."
                });
            }

            try
            {
                var client =
                    _httpClientFactory.CreateClient(
                        "AzureFunctions");

                string path =
                    "api/documents/download/" +
                    Uri.EscapeDataString(fileName);

                using var request =
                    new HttpRequestMessage(
                        HttpMethod.Get,
                        path);

                using var response =
                    await client.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                // If the Azure Function reports an error,
                // preserve its status and JSON response.
                if (!response.IsSuccessStatusCode)
                {
                    return await ForwardResponseAsync(
                        response,
                        cancellationToken);
                }

                // Obtain the actual file bytes.
                //
                // The current Azure Function returns
                // the document as an HTTP response body.
                byte[] fileBytes =
                    await response.Content.ReadAsByteArrayAsync(
                        cancellationToken);

                // Preserve the backend document MIME type.
                string contentType =
                    response.Content.Headers.ContentType?
                        .MediaType
                    ?? "application/octet-stream";

                // File(...) generates a downloadable
                // response instead of serialising bytes
                // as JSON.
                return File(
                    fileBytes,
                    contentType,
                    fileName);
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Staff document download timed out: {FileName}",
                    fileName);

                return StatusCode(
                    StatusCodes.Status504GatewayTimeout,
                    new
                    {
                        error =
                            "The document download service did not respond in time."
                    });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to reach Azure Functions while downloading {FileName}.",
                    fileName);

                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        error =
                            "The API Gateway could not reach Azure Functions."
                    });
            }
        }

        // =================================================
        // STEP 6: DELETE STAFF DOCUMENT
        // =================================================
        //
        // Gateway:
        // DELETE /api/documents/{fileName}
        //
        // Azure Function:
        // DELETE /api/documents/{fileName}
        //
        // Only Staff and Admin can delete documents.
        // =================================================

        /// <summary>
        /// Deletes a staff document from Azure Blob Storage.
        /// Requires Staff or Admin.
        /// </summary>
        [HttpDelete("{fileName}")]
        public async Task<IActionResult> Delete(
            string fileName,
            CancellationToken cancellationToken)
        {
            if (!IsValidFileName(fileName))
            {
                return BadRequest(new
                {
                    error = "Invalid document filename."
                });
            }

            string path =
                "api/documents/" +
                Uri.EscapeDataString(fileName);

            return await ForwardJsonRequestAsync(
                HttpMethod.Delete,
                path,
                cancellationToken);
        }

        // =================================================
        // STEP 7: VALIDATE DOCUMENT FILENAME
        // =================================================
        //
        // Prevent obviously invalid filenames from
        // being forwarded to the Azure Function.
        //
        // The storage service should independently
        // validate blob names as well.
        // =================================================

        private static bool IsValidFileName(
            string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            // Only a filename is accepted.
            // Directory separators are not allowed.
            if (fileName.Contains('/') ||
                fileName.Contains('\\') ||
                fileName.Contains(".."))
            {
                return false;
            }

            return fileName == Path.GetFileName(fileName);
        }

        // =================================================
        // STEP 8: SHARED JSON REQUEST FORWARDING
        // =================================================
        //
        // Used by:
        //
        // GET /api/documents
        // DELETE /api/documents/{fileName}
        //
        // Uses the same named HTTP client as
        // the existing MenuController.
        // =================================================

        private async Task<IActionResult>
            ForwardJsonRequestAsync(
                HttpMethod method,
                string functionPath,
                CancellationToken cancellationToken)
        {
            try
            {
                var client =
                    _httpClientFactory.CreateClient(
                        "AzureFunctions");

                using var request =
                    new HttpRequestMessage(
                        method,
                        functionPath);

                request.Headers.Accept.Add(
                    new MediaTypeWithQualityHeaderValue(
                        "application/json"));

                using var response =
                    await client.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                return await ForwardResponseAsync(
                    response,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Azure Functions request timed out: {Path}",
                    functionPath);

                return StatusCode(
                    StatusCodes.Status504GatewayTimeout,
                    new
                    {
                        error =
                            "The Azure Functions service did not respond in time."
                    });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to contact Azure Functions: {Path}",
                    functionPath);

                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        error =
                            "The API Gateway could not reach Azure Functions."
                    });
            }
        }

        // =================================================
        // STEP 9: PRESERVE BACKEND JSON RESPONSES
        // =================================================
        //
        // This method preserves:
        //
        // 1. HTTP status code.
        // 2. Response body.
        // 3. Response content type.
        //
        // ContentResult prevents JSON from being
        // accidentally serialised twice.
        // =================================================

        private async Task<IActionResult>
            ForwardResponseAsync(
                HttpResponseMessage response,
                CancellationToken cancellationToken)
        {
            if (response.StatusCode ==
                HttpStatusCode.NoContent)
            {
                return NoContent();
            }

            string responseBody =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            string contentType =
                response.Content.Headers.ContentType?
                    .ToString()
                ?? "application/json";

            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = responseBody,
                ContentType = contentType
            };
        }
    }
}

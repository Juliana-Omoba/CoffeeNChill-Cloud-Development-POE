using CoffeeNChill.Functions.DTOs.StaffDocuments;
using CoffeeNChill.Functions.Interfaces;
using CoffeeNChill.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace CoffeeNChill.Functions.Functions.StaffDocuments
{
    public class ListStaffDocumentsFunction
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<ListStaffDocumentsFunction> _logger;

        public ListStaffDocumentsFunction(
            IFileStorageService fileStorageService,
            ILogger<ListStaffDocumentsFunction> logger)
        {
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        [Function("ListStaffDocuments")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "documents")]
            HttpRequestData req)
        {
            _logger.LogInformation(
                "Retrieving all staff documents.");

            try
            {
                List<StaffDocument> documents =
                    await _fileStorageService.GetAllDocumentsAsync();

                List<StaffDocumentResponse> responseDtos =
                    documents.Select(document =>
                        new StaffDocumentResponse
                        {
                            FileName = document.FileName,
                            FileExtension = document.FileExtension,
                            ContentType = document.ContentType,
                            FileSize = document.FileSize,
                            UploadedOn = document.UploadedOn,
                            ContainerName = document.ContainerName
                        })
                    .ToList();

                _logger.LogInformation(
                    "Retrieved {Count} staff document(s).",
                    responseDtos.Count);

                var response =
                    req.CreateResponse(HttpStatusCode.OK);

                await response.WriteAsJsonAsync(responseDtos);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving all staff documents.");

                var response =
                    req.CreateResponse(
                        HttpStatusCode.InternalServerError);

                await response.WriteAsJsonAsync(new
                {
                    message =
                        "An unexpected error occurred while retrieving staff documents."
                });

                return response;
            }
        }
    }
}
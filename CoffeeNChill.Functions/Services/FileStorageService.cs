using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CoffeeNChill.Functions.Interfaces;
using CoffeeNChill.Functions.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace CoffeeNChill.Functions.Services
{
    public class FileStorageService :
        IFileStorageService
    {
        // --------------------------------------------------
        // Blob Storage container client
        // --------------------------------------------------
        private readonly BlobContainerClient
            _containerClient;

        // --------------------------------------------------
        // Name of the Blob container used for staff files
        // --------------------------------------------------
        private const string ContainerName =
            "staff-docs";

        // --------------------------------------------------
        // Constructor:
        // Connect to Blob Storage and create container
        // if it does not already exist
        // --------------------------------------------------
        public FileStorageService(
            IConfiguration configuration)
        {
            string connectionString =
                configuration[
                    "AzureWebJobsStorage"]
                ?? throw new
                    InvalidOperationException(
                        "AzureWebJobsStorage connection string is missing.");

            BlobServiceClient blobServiceClient =
                new BlobServiceClient(
                    connectionString);

            _containerClient =
                blobServiceClient
                    .GetBlobContainerClient(
                        ContainerName);

            _containerClient
                .CreateIfNotExists();
        }

        // --------------------------------------------------
        // Upload a staff document to Blob Storage
        // --------------------------------------------------
        public async Task<StaffDocument>
            UploadDocumentAsync(
                IFormFile file)
        {
            // Get a Blob reference using the file name
            BlobClient blobClient =
                _containerClient
                    .GetBlobClient(
                        file.FileName);

            // Open the uploaded file as a stream
            using Stream stream =
                file.OpenReadStream();

            // Store the file's MIME/content type
            BlobHttpHeaders headers =
                new BlobHttpHeaders
                {
                    ContentType =
                        file.ContentType
                };

            // Upload the file stream
            await blobClient.UploadAsync(
                stream,
                new BlobUploadOptions
                {
                    HttpHeaders =
                        headers
                });

            // Return document metadata
            return new StaffDocument
            {
                FileName =
                    file.FileName,

                FileExtension =
                    Path.GetExtension(
                        file.FileName),

                ContentType =
                    file.ContentType,

                FileSize =
                    file.Length,

                UploadedOn =
                    DateTime.UtcNow,

                ContainerName =
                    ContainerName
            };
        }

        // --------------------------------------------------
        // Download a document from Blob Storage
        // --------------------------------------------------
        public async Task<Stream?>
            DownloadDocumentAsync(
                string fileName)
        {
            BlobClient blobClient =
                _containerClient
                    .GetBlobClient(
                        fileName);

            // Return null when the Blob does not exist
            if (!await blobClient.ExistsAsync())
            {
                return null;
            }

            // Download the Blob as a stream
            BlobDownloadInfo download =
                await blobClient
                    .DownloadAsync();

            return download.Content;
        }

        // --------------------------------------------------
        // Delete a document from Blob Storage
        // --------------------------------------------------
        public async Task<bool>
            DeleteDocumentAsync(
                string fileName)
        {
            BlobClient blobClient =
                _containerClient
                    .GetBlobClient(
                        fileName);

            var response =
                await blobClient
                    .DeleteIfExistsAsync();

            return response.Value;
        }

        // --------------------------------------------------
        // Retrieve all documents and their metadata
        // --------------------------------------------------
        public async Task<List<StaffDocument>>
            GetAllDocumentsAsync()
        {
            List<StaffDocument> documents =
                new List<StaffDocument>();

            // Loop through every Blob in staff-docs
            await foreach (
                BlobItem item
                in _containerClient
                    .GetBlobsAsync())
            {
                BlobClient blobClient =
                    _containerClient
                        .GetBlobClient(
                            item.Name);

                // Retrieve Blob metadata/properties
                BlobProperties properties =
                    await blobClient
                        .GetPropertiesAsync();

                // Convert Blob information
                // into our application model
                documents.Add(
                    new StaffDocument
                    {
                        FileName =
                            item.Name,

                        FileExtension =
                            Path.GetExtension(
                                item.Name),

                        ContentType =
                            properties.ContentType
                            ?? string.Empty,

                        FileSize =
                            properties.ContentLength,

                        UploadedOn =
                            properties
                                .LastModified
                                .DateTime,

                        ContainerName =
                            ContainerName
                    });
            }

            return documents;
        }
    }
}
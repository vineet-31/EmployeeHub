using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace EmployeeHub.Services;

public class BlobStorageService
{
    private readonly BlobContainerClient _containerClient;

    public BlobStorageService(BlobServiceClient blobServiceClient, IConfiguration config)
    {
        var containerName = config["BlobStorage:ContainerName"]!;
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        _containerClient.CreateIfNotExists(PublicAccessType.Blob);
    }

    public async Task<string> UploadAsync(IFormFile file, string blobName)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
        });

        return blobClient.Uri.ToString();
    }

    public async Task DeleteAsync(string blobName)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync();
    }

    public string GetUrl(string blobName) => _containerClient.GetBlobClient(blobName).Uri.ToString();
}
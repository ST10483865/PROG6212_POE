using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay_api.Services;

/// <summary>
/// Azure Blob Storage implementation. The container is private: images are never exposed directly,
/// they are streamed to callers through the API endpoints.
/// Settings: AzureStorage:ConnectionString and AzureStorage:ContainerName.
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _container;

    public BlobStorageService(IConfiguration config)
    {
        var connectionString = config["AzureStorage:ConnectionString"];
        var containerName = config["AzureStorage:ContainerName"];
        if (string.IsNullOrWhiteSpace(containerName)) containerName = "raceday-images";

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "AzureStorage:ConnectionString is not configured. Add it with Manage User Secrets (see the README).");

        _container = new BlobContainerClient(connectionString, containerName);
    }

    public async Task<string> UploadAsync(string blobName, Stream content, string contentType, CancellationToken ct = default)
    {
        await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
        var blob = _container.GetBlobClient(blobName);
        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        }, ct);
        return blobName;
    }

    public async Task<(Stream Content, string ContentType)?> DownloadAsync(string blobName, CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient(blobName);
        if (!await blob.ExistsAsync(ct)) return null;

        var result = await blob.DownloadStreamingAsync(cancellationToken: ct);
        return (result.Value.Content, result.Value.Details.ContentType);
    }

    public async Task DeleteAsync(string blobName, CancellationToken ct = default)
    {
        await _container.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: ct);
    }
}


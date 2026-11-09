namespace RaceDay_api.Services
{//start of namespace
    public interface IBlobStorageService
    {
        //Uploads a file and returns the blob name it was stored under
        Task<string> UploadAsync(string blobName, Stream content, string contentType, CancellationToken ct = default);

        //Downloads a file, or returns null if it does not exist.
        Task<(Stream Content, string ContentType)?> DownloadAsync(string blobName, CancellationToken ct = default);

        // Deletes a file if it exists
        Task DeleteAsync(string blobName, CancellationToken ct = default);
    }
}//endo of namespace

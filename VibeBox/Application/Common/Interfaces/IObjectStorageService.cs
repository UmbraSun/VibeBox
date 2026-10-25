namespace Application.Common.Interfaces;

/// <summary>
/// Defines operations for uploading, downloading, and deleting objects in a storage service.
/// </summary>
public interface IObjectStorageService
{
    /// <summary>
    /// Asynchronously uploads content to the specified key with the given content type.
    /// </summary>
    /// <param name="key">The unique identifier for the uploaded content.</param>
    /// <param name="content">The stream containing the content to upload.</param>
    /// <param name="contentType">The MIME type of the content.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous upload operation.</returns>
    Task UploadAsync(
        string key,
        Stream content,
        string contentType,
        CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously downloads the stream associated with the specified key.
    /// </summary>
    /// <param name="key">The unique identifier for the resource to download.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the downloaded stream, or null if
    /// not found.</returns>
    Task<Stream?> DownloadAsync(
        string key,
        CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously deletes the item associated with the specified key.
    /// </summary>
    /// <param name="key">The unique identifier of the item to delete.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteAsync(
        string key,
        CancellationToken cancellationToken);
}
namespace AuditTrail.Core.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file and returns the file path/key
    /// </summary>
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Downloads a file as a stream
    /// </summary>
    Task<Stream> DownloadFileAsync(string filePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a file
    /// </summary>
    Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a file exists
    /// </summary>
    Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the file size in bytes
    /// </summary>
    Task<long> GetFileSizeAsync(string filePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates a pre-signed URL for direct download (for S3) or returns local path (for local storage)
    /// </summary>
    Task<string> GetDownloadUrlAsync(string filePath, TimeSpan expiration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Copies a file to another location
    /// </summary>
    Task<string> CopyFileAsync(string sourceFilePath, string destinationFilePath, CancellationToken cancellationToken = default);
}
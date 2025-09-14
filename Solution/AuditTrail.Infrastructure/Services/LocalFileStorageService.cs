using AuditTrail.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuditTrail.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly string _basePath;

    public LocalFileStorageService(ILogger<LocalFileStorageService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _basePath = configuration["FileStorage:LocalPath"] 
                   ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

        // Ensure the base directory exists
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
            _logger.LogInformation("Created file storage directory: {BasePath}", _basePath);
        }
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            // Generate unique file name to avoid conflicts
            var fileExtension = Path.GetExtension(fileName);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var uniqueFileName = $"{fileNameWithoutExtension}_{Guid.NewGuid():N}{fileExtension}";

            // Create subdirectory based on date for organization
            var dateFolder = DateTime.UtcNow.ToString("yyyy/MM/dd");
            var fullDirectory = Path.Combine(_basePath, dateFolder);
            
            if (!Directory.Exists(fullDirectory))
            {
                Directory.CreateDirectory(fullDirectory);
            }

            var fullPath = Path.Combine(fullDirectory, uniqueFileName);
            var relativePath = Path.Combine(dateFolder, uniqueFileName).Replace('\\', '/');

            using var fileStream2 = new FileStream(fullPath, FileMode.Create);
            await fileStream.CopyToAsync(fileStream2, cancellationToken);

            _logger.LogInformation("File uploaded successfully: {RelativePath}", relativePath);
            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", fileName);
            throw;
        }
    }

    public async Task<Stream> DownloadFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, filePath);
            
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            return await Task.FromResult(fileStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file: {FilePath}", filePath);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, filePath);
            
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                return true;
            }

            return await Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
            return false;
        }
    }

    public async Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, filePath);
            return await Task.FromResult(File.Exists(fullPath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file existence: {FilePath}", filePath);
            return false;
        }
    }

    public async Task<long> GetFileSizeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, filePath);
            
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var fileInfo = new FileInfo(fullPath);
            return await Task.FromResult(fileInfo.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file size: {FilePath}", filePath);
            throw;
        }
    }

    public async Task<string> GetDownloadUrlAsync(string filePath, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        try
        {
            // For local storage, we return a controller action URL that will serve the file
            // This simulates what S3 pre-signed URLs do
            var encodedPath = Uri.EscapeDataString(filePath);
            return await Task.FromResult($"/Documents/DownloadFile?filePath={encodedPath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating download URL: {FilePath}", filePath);
            throw;
        }
    }

    public async Task<string> CopyFileAsync(string sourceFilePath, string destinationFilePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var sourceFullPath = Path.Combine(_basePath, sourceFilePath);
            var destinationFullPath = Path.Combine(_basePath, destinationFilePath);

            if (!File.Exists(sourceFullPath))
            {
                throw new FileNotFoundException($"Source file not found: {sourceFilePath}");
            }

            // Ensure destination directory exists
            var destinationDirectory = Path.GetDirectoryName(destinationFullPath);
            if (!string.IsNullOrEmpty(destinationDirectory) && !Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            File.Copy(sourceFullPath, destinationFullPath, overwrite: true);
            
            _logger.LogInformation("File copied from {Source} to {Destination}", sourceFilePath, destinationFilePath);
            return await Task.FromResult(destinationFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying file from {Source} to {Destination}", sourceFilePath, destinationFilePath);
            throw;
        }
    }

    /// <summary>
    /// Gets the physical file path for local operations (testing purposes)
    /// </summary>
    public string GetPhysicalPath(string filePath)
    {
        return Path.Combine(_basePath, filePath);
    }
}
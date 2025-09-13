using AuditTrail.Core.Entities.Documents;

namespace AuditTrail.Core.Interfaces;

public interface IFileRepository : IRepository<FileEntity>
{
    Task<FileEntity?> GetFileWithVersionsAsync(Guid fileId);
    Task<FileEntity?> GetLatestVersionAsync(string filePath, string fileName);
    Task<int> GetNextVersionNumberAsync(string filePath, string fileName);
    Task<FileEntity> UploadFileVersionAsync(
        FileEntity file,
        Guid uploadedBy,
        string? versionComment = null);
    Task<IEnumerable<FileEntity>> GetFilesByPathAsync(string path, bool includeDeleted = false);
    Task<IEnumerable<FileEntity>> GetFilesByCategoryAsync(int categoryId, bool includeSubcategories = false);
    Task<IEnumerable<FileEntity>> GetUserFilesAsync(Guid userId, int? limit = null);
    Task<bool> UserHasAccessAsync(Guid userId, Guid fileId, int requiredPermission);
    Task SoftDeleteFileAsync(Guid fileId, Guid deletedBy, string reason);
    Task ArchiveFileAsync(Guid fileId, Guid archivedBy, string storageTier);
}
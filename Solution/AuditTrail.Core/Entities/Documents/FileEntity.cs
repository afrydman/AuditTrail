namespace AuditTrail.Core.Entities.Documents;

public class FileEntity : BaseEntityWithId  
{
    public string FileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public int Version { get; set; } = 1;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string BlobUrl { get; set; } = string.Empty;
    public string S3BucketName { get; set; } = string.Empty;
    public string S3Key { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;
    public string ChecksumAlgorithm { get; set; } = "SHA256";
    public string OriginalFileName { get; set; } = string.Empty;
    public string? StudyId { get; set; }
    public string? DocumentType { get; set; }
    public string? Tags { get; set; }
    public string? Description { get; set; }
    public bool IsEncrypted { get; set; } = true;
    public string? EncryptionKeyId { get; set; }
    public Guid UploadedBy { get; set; }
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedDate { get; set; }
    public Guid? DeletedBy { get; set; }
    public string? DeleteReason { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedDate { get; set; }
    public Guid? ArchivedBy { get; set; }
    public string? ArchiveStorageTier { get; set; }
    
    // Navigation properties
    public virtual FileCategory? Category { get; set; }
    public virtual ICollection<FileVersion> Versions { get; set; } = new List<FileVersion>();
}
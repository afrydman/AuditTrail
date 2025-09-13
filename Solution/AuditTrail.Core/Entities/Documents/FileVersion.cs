namespace AuditTrail.Core.Entities.Documents;

public class FileVersion
{
    public Guid VersionId { get; set; } = Guid.NewGuid();
    public Guid FileId { get; set; }
    public int Version { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string BlobUrl { get; set; } = string.Empty;
    public string S3Key { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;
    public string? VersionComment { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual FileEntity? File { get; set; }
}
namespace AuditTrail.Web.Models
{
    public class FileCategoryDto
    {
        public int Id { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryPath { get; set; } = string.Empty;
        public int? ParentCategoryId { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool InheritParentPermissions { get; set; }
        public bool RequireExplicitAccess { get; set; }
        public bool IsSystemFolder { get; set; }
        public Guid CreatedBy { get; set; }
    }

    public class FileEntityDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public int Version { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public Guid UploadedBy { get; set; }
        public DateTime UploadedDate { get; set; }
        public bool IsDeleted { get; set; }
    }
}
namespace AuditTrail.Core.Entities.Documents;

public class FileCategory : BaseEntityWithIntId
{
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryPath { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool InheritParentPermissions { get; set; } = true;
    public bool RequireExplicitAccess { get; set; }
    public bool IsSystemFolder { get; set; }
    
    // Navigation properties
    public virtual FileCategory? ParentCategory { get; set; }
    public virtual ICollection<FileCategory> SubCategories { get; set; } = new List<FileCategory>();
    public virtual ICollection<FileEntity> Files { get; set; } = new List<FileEntity>();
    public virtual ICollection<CategoryAccess> CategoryAccesses { get; set; } = new List<CategoryAccess>();
}
using AuditTrail.Core.Entities.Documents;

namespace AuditTrail.Web.Models
{
    public class DocumentsIndexViewModel
    {
        public List<FileCategory> Categories { get; set; } = new List<FileCategory>();
        public List<FileEntity> Files { get; set; } = new List<FileEntity>();
    }

    public class TreeNodeViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsExpandable { get; set; }
        public bool IsCategory { get; set; }
        public List<TreeNodeViewModel>? Children { get; set; }
        
        // Category-specific properties
        public int? CategoryId { get; set; }
        public string? Description { get; set; }
        
        // File-specific properties
        public Guid? FileId { get; set; }
        public long? FileSize { get; set; }
        public string? FileExtension { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
namespace AuditTrail.Core.Entities.Documents;

using AuditTrail.Core.Entities.Auth;

public class CategoryAccess
{
    public int CategoryAccessId { get; set; }
    public int CategoryId { get; set; }
    public Guid? UserId { get; set; }
    public int? RoleId { get; set; }
    public int Permissions { get; set; } // Bitwise permissions
    public bool InheritToSubfolders { get; set; } = true;
    public bool InheritToFiles { get; set; } = true;
    public Guid GrantedBy { get; set; }
    public DateTime GrantedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? RevokedBy { get; set; }
    public DateTime? RevokedDate { get; set; }
    public string? RevokeReason { get; set; }
    
    // Navigation properties
    public virtual FileCategory? Category { get; set; }
    public virtual User? User { get; set; }
    public virtual Role? Role { get; set; }
}
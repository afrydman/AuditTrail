namespace AuditTrail.Core.Entities.Auth;

public class RolePermission
{
    public int RolePermissionId { get; set; }
    public int RoleId { get; set; }
    public int PermissionId { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    public int AccessLevel { get; set; }
    public Guid GrantedBy { get; set; }
    public DateTime GrantedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresDate { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? RevokedBy { get; set; }
    public DateTime? RevokedDate { get; set; }
    public string? RevokeReason { get; set; }
    
    // Navigation properties
    public virtual Role? Role { get; set; }
    public virtual Permission? Permission { get; set; }
}
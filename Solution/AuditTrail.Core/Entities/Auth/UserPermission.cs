namespace AuditTrail.Core.Entities.Auth;

public class UserPermission
{
    public int UserPermissionId { get; set; }
    public Guid UserId { get; set; }
    public int PermissionId { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    public int AccessLevel { get; set; }
    public bool IsGrant { get; set; } = true;
    public Guid GrantedBy { get; set; }
    public DateTime GrantedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresDate { get; set; }
    public string? Reason { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? RevokedBy { get; set; }
    public DateTime? RevokedDate { get; set; }
    
    // Navigation properties
    public virtual User? User { get; set; }
    public virtual Permission? Permission { get; set; }
}
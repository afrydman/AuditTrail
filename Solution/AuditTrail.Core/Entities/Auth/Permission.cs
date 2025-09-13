namespace AuditTrail.Core.Entities.Auth;

public class Permission : BaseEntityWithIntId
{
    public string PermissionName { get; set; } = string.Empty;
    public string PermissionCode { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}
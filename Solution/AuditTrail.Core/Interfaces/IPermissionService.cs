using AuditTrail.Core.Entities.Documents;
using AuditTrail.Core.Enums;

namespace AuditTrail.Core.Interfaces;

public interface IPermissionService
{
    /// <summary>
    /// Gets effective permissions for a user on a specific folder
    /// </summary>
    Task<FilePermissions> GetFolderPermissionsAsync(int categoryId, Guid userId);
    
    /// <summary>
    /// Gets effective permissions for a user on a specific file
    /// </summary>
    Task<FilePermissions> GetFilePermissionsAsync(Guid fileId, Guid userId);
    
    /// <summary>
    /// Checks if a user has a specific permission on a folder
    /// </summary>
    Task<bool> HasFolderPermissionAsync(int categoryId, Guid userId, FilePermissions permission);
    
    /// <summary>
    /// Checks if a user has a specific permission on a file
    /// </summary>
    Task<bool> HasFilePermissionAsync(Guid fileId, Guid userId, FilePermissions permission);
    
    /// <summary>
    /// Gets all permission assignments for a specific folder
    /// </summary>
    Task<List<CategoryAccess>> GetFolderPermissionsAsync(int categoryId);
    
    /// <summary>
    /// Grants permissions to a role for a specific folder
    /// </summary>
    Task GrantFolderPermissionAsync(int categoryId, int roleId, FilePermissions permissions, Guid grantedBy);
    
    /// <summary>
    /// Revokes permissions from a role for a specific folder  
    /// </summary>
    Task RevokeFolderPermissionAsync(int categoryId, int roleId, Guid revokedBy, string reason);
    
    /// <summary>
    /// Updates permissions for a role on a specific folder
    /// </summary>
    Task UpdateFolderPermissionAsync(int categoryId, int roleId, FilePermissions permissions, Guid modifiedBy);
    
    /// <summary>
    /// Gets permissions inherited from parent folders
    /// </summary>
    Task<FilePermissions> GetInheritedPermissionsAsync(int categoryId, Guid userId);
    
    /// <summary>
    /// Checks if user can delete a folder (special handling for root folders)
    /// </summary>
    Task<bool> CanDeleteFolderAsync(int categoryId, Guid userId);
}
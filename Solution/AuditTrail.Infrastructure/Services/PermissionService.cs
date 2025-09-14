using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AuditTrail.Core.Interfaces;
using AuditTrail.Core.Entities.Documents;
using AuditTrail.Core.Enums;
using AuditTrail.Infrastructure.Data;

namespace AuditTrail.Infrastructure.Services;

public class PermissionService : IPermissionService
{
    private readonly AuditTrailDbContext _context;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(AuditTrailDbContext context, ILogger<PermissionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<FilePermissions> GetFolderPermissionsAsync(int categoryId, Guid userId)
    {
        try
        {
            // Get user's role
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user == null) return FilePermissions.None;

            int effectivePermissions = 0;

            // Get direct permissions for the user's role on this folder
            var rolePermissions = await _context.CategoryAccesses
                .Where(ca => ca.CategoryId == categoryId && 
                            ca.RoleId == user.RoleId && 
                            ca.IsActive && 
                            (ca.ExpiryDate == null || ca.ExpiryDate > DateTime.UtcNow))
                .Select(ca => ca.Permissions)
                .FirstOrDefaultAsync();

            effectivePermissions |= rolePermissions;

            // Get inherited permissions from parent folders
            var inheritedPermissions = await GetInheritedPermissionsAsync(categoryId, userId);
            effectivePermissions |= (int)inheritedPermissions;

            return (FilePermissions)effectivePermissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting folder permissions for user {UserId} on category {CategoryId}", userId, categoryId);
            return FilePermissions.None;
        }
    }

    public async Task<FilePermissions> GetFilePermissionsAsync(Guid fileId, Guid userId)
    {
        try
        {
            // Get file's category
            var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == fileId);
            if (file?.CategoryId == null) return FilePermissions.None;

            // Files inherit permissions from their folder
            return await GetFolderPermissionsAsync(file.CategoryId.Value, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file permissions for user {UserId} on file {FileId}", userId, fileId);
            return FilePermissions.None;
        }
    }

    public async Task<bool> HasFolderPermissionAsync(int categoryId, Guid userId, FilePermissions permission)
    {
        var userPermissions = await GetFolderPermissionsAsync(categoryId, userId);
        return userPermissions.HasFlag(permission);
    }

    public async Task<bool> HasFilePermissionAsync(Guid fileId, Guid userId, FilePermissions permission)
    {
        var userPermissions = await GetFilePermissionsAsync(fileId, userId);
        return userPermissions.HasFlag(permission);
    }

    public async Task<List<CategoryAccess>> GetFolderPermissionsAsync(int categoryId)
    {
        return await _context.CategoryAccesses
            .Include(ca => ca.Role)
            .Include(ca => ca.User)
            .Where(ca => ca.CategoryId == categoryId && ca.IsActive)
            .OrderBy(ca => ca.RoleId)
            .ToListAsync();
    }

    public async Task GrantFolderPermissionAsync(int categoryId, int roleId, FilePermissions permissions, Guid grantedBy)
    {
        try
        {
            // Check if permission already exists
            var existing = await _context.CategoryAccesses
                .FirstOrDefaultAsync(ca => ca.CategoryId == categoryId && ca.RoleId == roleId && ca.IsActive);

            if (existing != null)
            {
                // Update existing permission
                existing.Permissions = (int)permissions;
                existing.GrantedBy = grantedBy;
                existing.GrantedDate = DateTime.UtcNow;
            }
            else
            {
                // Create new permission
                var newPermission = new CategoryAccess
                {
                    CategoryId = categoryId,
                    RoleId = roleId,
                    Permissions = (int)permissions,
                    GrantedBy = grantedBy,
                    GrantedDate = DateTime.UtcNow,
                    IsActive = true,
                    InheritToSubfolders = true,
                    InheritToFiles = true
                };

                _context.CategoryAccesses.Add(newPermission);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Granted permissions {Permissions} to role {RoleId} on category {CategoryId} by user {GrantedBy}", 
                permissions, roleId, categoryId, grantedBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error granting folder permission");
            throw;
        }
    }

    public async Task RevokeFolderPermissionAsync(int categoryId, int roleId, Guid revokedBy, string reason)
    {
        try
        {
            var permission = await _context.CategoryAccesses
                .FirstOrDefaultAsync(ca => ca.CategoryId == categoryId && ca.RoleId == roleId && ca.IsActive);

            if (permission != null)
            {
                permission.IsActive = false;
                permission.RevokedBy = revokedBy;
                permission.RevokedDate = DateTime.UtcNow;
                permission.RevokeReason = reason;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Revoked permissions for role {RoleId} on category {CategoryId} by user {RevokedBy}", 
                    roleId, categoryId, revokedBy);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking folder permission");
            throw;
        }
    }

    public async Task UpdateFolderPermissionAsync(int categoryId, int roleId, FilePermissions permissions, Guid modifiedBy)
    {
        await GrantFolderPermissionAsync(categoryId, roleId, permissions, modifiedBy);
    }

    public async Task<FilePermissions> GetInheritedPermissionsAsync(int categoryId, Guid userId)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user == null) return FilePermissions.None;

            int inheritedPermissions = 0;

            // Get all parent categories that this category inherits from
            var category = await _context.FileCategories.FirstOrDefaultAsync(c => c.Id == categoryId);
            if (category?.ParentCategoryId == null || !category.InheritParentPermissions)
                return FilePermissions.None;

            // Traverse up the parent chain
            var currentCategoryId = category.ParentCategoryId;
            while (currentCategoryId.HasValue)
            {
                var parentPermissions = await _context.CategoryAccesses
                    .Where(ca => ca.CategoryId == currentCategoryId.Value && 
                                ca.RoleId == user.RoleId && 
                                ca.IsActive && 
                                ca.InheritToSubfolders &&
                                (ca.ExpiryDate == null || ca.ExpiryDate > DateTime.UtcNow))
                    .Select(ca => ca.Permissions)
                    .FirstOrDefaultAsync();

                inheritedPermissions |= parentPermissions;

                // Move to next parent
                var parentCategory = await _context.FileCategories
                    .FirstOrDefaultAsync(c => c.Id == currentCategoryId.Value);
                
                if (parentCategory?.ParentCategoryId == null || !parentCategory.InheritParentPermissions)
                    break;

                currentCategoryId = parentCategory.ParentCategoryId;
            }

            return (FilePermissions)inheritedPermissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inherited permissions for category {CategoryId} and user {UserId}", categoryId, userId);
            return FilePermissions.None;
        }
    }

    public async Task<bool> CanDeleteFolderAsync(int categoryId, Guid userId)
    {
        try
        {
            // Get the folder
            var category = await _context.FileCategories.FirstOrDefaultAsync(c => c.Id == categoryId);
            if (category == null) return false;

            // Check if it's a root folder (no parent)
            if (category.ParentCategoryId == null)
            {
                // Root folders can only be deleted by administrators
                var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
                return user?.Role?.RoleName?.ToLower() == "administrator";
            }

            // For non-root folders, check regular delete permissions
            return await HasFolderPermissionAsync(categoryId, userId, FilePermissions.Delete);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user can delete folder {CategoryId}", categoryId);
            return false;
        }
    }
}
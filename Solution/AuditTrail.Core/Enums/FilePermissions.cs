namespace AuditTrail.Core.Enums;

/// <summary>
/// File and folder permission flags (bitwise)
/// </summary>
[Flags]
public enum FilePermissions
{
    None = 0,
    View = 1,           // 0x01 - Can see file/folder exists
    Download = 2,       // 0x02 - Can download/read file contents  
    Upload = 4,         // 0x04 - Can upload new files to folder
    Delete = 8,         // 0x08 - Can delete files/folders
    ModifyMetadata = 16, // 0x10 - Can edit file properties
    Admin = 32          // 0x20 - Can manage permissions and full control
}

/// <summary>
/// Common permission combinations for easier management
/// </summary>
public static class CommonPermissions
{
    public const int ViewOnly = (int)(FilePermissions.View);
    public const int ReadOnly = (int)(FilePermissions.View | FilePermissions.Download);
    public const int ReadWrite = (int)(FilePermissions.View | FilePermissions.Download | FilePermissions.Upload);
    public const int Editor = (int)(FilePermissions.View | FilePermissions.Download | FilePermissions.Upload | FilePermissions.Delete | FilePermissions.ModifyMetadata);
    public const int FullControl = (int)(FilePermissions.View | FilePermissions.Download | FilePermissions.Upload | FilePermissions.Delete | FilePermissions.ModifyMetadata | FilePermissions.Admin);
}
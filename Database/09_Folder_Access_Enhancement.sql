-- =============================================
-- Folder Access Control Enhancement
-- Adds folder-level permissions with inheritance
-- Schema: docs
-- =============================================

SET NOCOUNT ON;
USE [AuditTrail];
GO

PRINT 'Starting folder access enhancement...';

BEGIN TRANSACTION;
BEGIN TRY

    -- =============================================
    -- Table: CategoryAccess (Folder-level permissions)
    -- =============================================
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CategoryAccess' AND schema_id = SCHEMA_ID('docs'))
    BEGIN
        PRINT 'Creating table [docs].[CategoryAccess]...';
        
        CREATE TABLE [docs].[CategoryAccess] (
            CategoryAccessId INT IDENTITY(1,1) NOT NULL,
            CategoryId INT NOT NULL,
            UserId UNIQUEIDENTIFIER NULL,
            RoleId INT NULL,
            -- Bitwise permissions: 1=View, 2=Download, 4=Upload, 8=Delete, 16=ModifyMetadata, 32=ManagePermissions
            Permissions INT NOT NULL,
            InheritToSubfolders BIT NOT NULL DEFAULT 1,
            InheritToFiles BIT NOT NULL DEFAULT 1,
            GrantedBy UNIQUEIDENTIFIER NOT NULL,
            GrantedDate DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
            ExpiryDate DATETIME2(7) NULL,
            IsActive BIT NOT NULL DEFAULT 1,
            RevokedBy UNIQUEIDENTIFIER NULL,
            RevokedDate DATETIME2(7) NULL,
            RevokeReason NVARCHAR(500) NULL,
            
            CONSTRAINT PK_CategoryAccess PRIMARY KEY CLUSTERED (CategoryAccessId),
            CONSTRAINT FK_CategoryAccess_Category FOREIGN KEY (CategoryId) 
                REFERENCES [docs].[FileCategories](CategoryId),
            CONSTRAINT FK_CategoryAccess_User FOREIGN KEY (UserId) 
                REFERENCES [auth].[Users](UserId),
            CONSTRAINT FK_CategoryAccess_Role FOREIGN KEY (RoleId) 
                REFERENCES [auth].[Roles](RoleId),
            CONSTRAINT FK_CategoryAccess_GrantedBy FOREIGN KEY (GrantedBy) 
                REFERENCES [auth].[Users](UserId),
            CONSTRAINT CHK_CategoryAccess_UserOrRole 
                CHECK ((UserId IS NOT NULL AND RoleId IS NULL) OR (UserId IS NULL AND RoleId IS NOT NULL))
        );
        
        -- Create indexes for performance
        CREATE NONCLUSTERED INDEX IX_CategoryAccess_CategoryId 
        ON [docs].[CategoryAccess](CategoryId, IsActive) 
        INCLUDE (UserId, RoleId, Permissions, InheritToSubfolders);
        
        CREATE NONCLUSTERED INDEX IX_CategoryAccess_UserId 
        ON [docs].[CategoryAccess](UserId, IsActive) 
        INCLUDE (CategoryId, Permissions)
        WHERE UserId IS NOT NULL;
        
        CREATE NONCLUSTERED INDEX IX_CategoryAccess_RoleId 
        ON [docs].[CategoryAccess](RoleId, IsActive) 
        INCLUDE (CategoryId, Permissions)
        WHERE RoleId IS NOT NULL;
        
        PRINT 'Table [docs].[CategoryAccess] created successfully.';
    END

    -- =============================================
    -- Add columns to FileCategories for better access control
    -- =============================================
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[docs].[FileCategories]') AND name = 'InheritParentPermissions')
    BEGIN
        ALTER TABLE [docs].[FileCategories] 
        ADD InheritParentPermissions BIT NOT NULL DEFAULT 1;
        PRINT 'Added InheritParentPermissions column to FileCategories.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[docs].[FileCategories]') AND name = 'RequireExplicitAccess')
    BEGIN
        ALTER TABLE [docs].[FileCategories] 
        ADD RequireExplicitAccess BIT NOT NULL DEFAULT 0;
        PRINT 'Added RequireExplicitAccess column to FileCategories.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[docs].[FileCategories]') AND name = 'IsSystemFolder')
    BEGIN
        ALTER TABLE [docs].[FileCategories] 
        ADD IsSystemFolder BIT NOT NULL DEFAULT 0;
        PRINT 'Added IsSystemFolder column to FileCategories.';
    END

    -- =============================================
    -- Table: EffectivePermissions (Materialized view for performance)
    -- =============================================
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EffectivePermissions' AND schema_id = SCHEMA_ID('docs'))
    BEGIN
        PRINT 'Creating table [docs].[EffectivePermissions]...';
        
        CREATE TABLE [docs].[EffectivePermissions] (
            EffectivePermissionId BIGINT IDENTITY(1,1) NOT NULL,
            ResourceType NVARCHAR(20) NOT NULL, -- 'File' or 'Category'
            ResourceId NVARCHAR(100) NOT NULL,
            UserId UNIQUEIDENTIFIER NOT NULL,
            Permissions INT NOT NULL,
            SourceType NVARCHAR(50) NOT NULL, -- 'Direct', 'RoleBased', 'Inherited'
            SourceId INT NULL, -- References the access rule that granted this permission
            LastCalculated DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
            
            CONSTRAINT PK_EffectivePermissions PRIMARY KEY CLUSTERED (EffectivePermissionId),
            CONSTRAINT FK_EffectivePermissions_User FOREIGN KEY (UserId) 
                REFERENCES [auth].[Users](UserId)
        );
        
        -- Create indexes for fast lookups
        CREATE UNIQUE NONCLUSTERED INDEX UQ_EffectivePermissions_UserResource
        ON [docs].[EffectivePermissions](UserId, ResourceType, ResourceId)
        INCLUDE (Permissions, SourceType);
        
        CREATE NONCLUSTERED INDEX IX_EffectivePermissions_Resource
        ON [docs].[EffectivePermissions](ResourceType, ResourceId)
        INCLUDE (UserId, Permissions);
        
        PRINT 'Table [docs].[EffectivePermissions] created successfully.';
    END

    COMMIT TRANSACTION;
    PRINT 'Folder access enhancement completed successfully.';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'ERROR in folder access enhancement:';
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS NVARCHAR(10));
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    RETURN;
END CATCH
GO

-- =============================================
-- Stored Procedure: Calculate Effective Permissions
-- =============================================
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_CalculateEffectivePermissions')
    DROP PROCEDURE [docs].[sp_CalculateEffectivePermissions];
GO

CREATE PROCEDURE [docs].[sp_CalculateEffectivePermissions]
    @UserId UNIQUEIDENTIFIER,
    @ResourceType NVARCHAR(20), -- 'File' or 'Category'
    @ResourceId NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Permissions INT = 0;
    DECLARE @RoleId INT;
    DECLARE @CategoryId INT;
    DECLARE @ParentCategoryId INT;
    
    -- Get user's role
    SELECT @RoleId = RoleId 
    FROM [auth].[Users] 
    WHERE UserId = @UserId AND IsActive = 1;
    
    IF @ResourceType = 'Category'
    BEGIN
        SET @CategoryId = CAST(@ResourceId AS INT);
        
        -- Check direct user permissions on category
        SELECT @Permissions = @Permissions | Permissions
        FROM [docs].[CategoryAccess]
        WHERE CategoryId = @CategoryId 
            AND UserId = @UserId 
            AND IsActive = 1
            AND (ExpiryDate IS NULL OR ExpiryDate > GETUTCDATE());
        
        -- Check role-based permissions on category
        SELECT @Permissions = @Permissions | Permissions
        FROM [docs].[CategoryAccess]
        WHERE CategoryId = @CategoryId 
            AND RoleId = @RoleId 
            AND IsActive = 1
            AND (ExpiryDate IS NULL OR ExpiryDate > GETUTCDATE());
        
        -- Check inherited permissions from parent folders
        WITH CategoryHierarchy AS (
            SELECT CategoryId, ParentCategoryId, InheritParentPermissions
            FROM [docs].[FileCategories]
            WHERE CategoryId = @CategoryId
            
            UNION ALL
            
            SELECT p.CategoryId, p.ParentCategoryId, p.InheritParentPermissions
            FROM [docs].[FileCategories] p
            INNER JOIN CategoryHierarchy c ON p.CategoryId = c.ParentCategoryId
            WHERE c.InheritParentPermissions = 1
        )
        SELECT @Permissions = @Permissions | ca.Permissions
        FROM CategoryHierarchy ch
        INNER JOIN [docs].[CategoryAccess] ca ON ca.CategoryId = ch.CategoryId
        WHERE ca.IsActive = 1
            AND ca.InheritToSubfolders = 1
            AND (ca.UserId = @UserId OR ca.RoleId = @RoleId)
            AND (ca.ExpiryDate IS NULL OR ca.ExpiryDate > GETUTCDATE());
    END
    ELSE IF @ResourceType = 'File'
    BEGIN
        DECLARE @FileId UNIQUEIDENTIFIER = CAST(@ResourceId AS UNIQUEIDENTIFIER);
        
        -- Get file's category
        SELECT @CategoryId = CategoryId
        FROM [docs].[Files]
        WHERE FileId = @FileId;
        
        -- Check direct file permissions
        SELECT @Permissions = @Permissions | 
            CASE AccessLevel
                WHEN 'Read' THEN 1
                WHEN 'Write' THEN 7  -- View + Download + Upload
                WHEN 'Full' THEN 63  -- All permissions
                ELSE 0
            END
        FROM [docs].[FileAccess]
        WHERE FileId = @FileId 
            AND (UserId = @UserId OR RoleId = @RoleId)
            AND IsActive = 1
            AND (ExpiryDate IS NULL OR ExpiryDate > GETUTCDATE());
        
        -- Check inherited permissions from folder
        IF @CategoryId IS NOT NULL
        BEGIN
            WITH CategoryHierarchy AS (
                SELECT CategoryId, ParentCategoryId, InheritParentPermissions
                FROM [docs].[FileCategories]
                WHERE CategoryId = @CategoryId
                
                UNION ALL
                
                SELECT p.CategoryId, p.ParentCategoryId, p.InheritParentPermissions
                FROM [docs].[FileCategories] p
                INNER JOIN CategoryHierarchy c ON p.CategoryId = c.ParentCategoryId
                WHERE c.InheritParentPermissions = 1
            )
            SELECT @Permissions = @Permissions | ca.Permissions
            FROM CategoryHierarchy ch
            INNER JOIN [docs].[CategoryAccess] ca ON ca.CategoryId = ch.CategoryId
            WHERE ca.IsActive = 1
                AND ca.InheritToFiles = 1
                AND (ca.UserId = @UserId OR ca.RoleId = @RoleId)
                AND (ca.ExpiryDate IS NULL OR ca.ExpiryDate > GETUTCDATE());
        END
    END
    
    -- Update or insert into EffectivePermissions table
    MERGE [docs].[EffectivePermissions] AS target
    USING (SELECT @UserId AS UserId, @ResourceType AS ResourceType, @ResourceId AS ResourceId) AS source
    ON target.UserId = source.UserId 
        AND target.ResourceType = source.ResourceType 
        AND target.ResourceId = source.ResourceId
    WHEN MATCHED THEN
        UPDATE SET 
            Permissions = @Permissions,
            LastCalculated = GETUTCDATE()
    WHEN NOT MATCHED THEN
        INSERT (ResourceType, ResourceId, UserId, Permissions, SourceType)
        VALUES (@ResourceType, @ResourceId, @UserId, @Permissions, 'Calculated');
    
    SELECT @Permissions AS EffectivePermissions;
END
GO

-- =============================================
-- Permission Constants (for reference in application code)
-- =============================================
/*
PERMISSION FLAGS (Bitwise):
- View           = 1    (0x01) - Can see file/folder exists
- Download       = 2    (0x02) - Can download/read file contents
- Upload         = 4    (0x04) - Can upload new files to folder
- Delete         = 8    (0x08) - Can delete files/folders
- ModifyMetadata = 16   (0x10) - Can edit file properties
- ManagePermissions = 32 (0x20) - Can grant/revoke access to others

COMMON COMBINATIONS:
- Read Only      = 3    (View + Download)
- Contributor    = 7    (View + Download + Upload)
- Editor         = 23   (View + Download + Upload + Delete + ModifyMetadata)
- Manager        = 63   (All permissions)

USAGE EXAMPLE:
-- Grant read-only access to a folder for a role
INSERT INTO [docs].[CategoryAccess] (CategoryId, RoleId, Permissions, GrantedBy)
VALUES (1, 5, 3, 'admin-user-guid');

-- Check if user can upload to folder
IF (@Permissions & 4) = 4
    -- User has upload permission
*/

PRINT 'Folder access enhancement script completed.';
PRINT 'Remember to grant appropriate permissions to folders after running this script.';
GO
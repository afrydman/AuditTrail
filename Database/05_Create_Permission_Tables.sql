-- =============================================
-- Permission and Access Control Tables
-- Schema: auth
-- Re-runnable: YES
-- =============================================

SET NOCOUNT ON;
USE [AuditTrail];
GO

PRINT 'Starting permission tables creation...';

BEGIN TRANSACTION;
BEGIN TRY

    -- =============================================
    -- Table: Permissions
    -- =============================================
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Permissions' AND schema_id = SCHEMA_ID('auth'))
    BEGIN
        PRINT 'Creating table [auth].[Permissions]...';
        
        CREATE TABLE [auth].[Permissions] (
            PermissionId INT IDENTITY(1,1) NOT NULL,
            PermissionName NVARCHAR(100) NOT NULL,
            PermissionCode NVARCHAR(50) NOT NULL,
            ResourceType NVARCHAR(50) NOT NULL,
            Description NVARCHAR(500) NULL,
            IsActive BIT NOT NULL DEFAULT 1,
            
            CONSTRAINT PK_Permissions PRIMARY KEY CLUSTERED (PermissionId),
            CONSTRAINT UQ_Permissions_Code UNIQUE (PermissionCode)
        );
        PRINT 'Table [auth].[Permissions] created successfully.';
    END

    -- =============================================
    -- Table: RolePermissions
    -- =============================================
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RolePermissions' AND schema_id = SCHEMA_ID('auth'))
    BEGIN
        PRINT 'Creating table [auth].[RolePermissions]...';
        
        CREATE TABLE [auth].[RolePermissions] (
            RolePermissionId INT IDENTITY(1,1) NOT NULL,
            RoleId INT NOT NULL,
            PermissionId INT NOT NULL,
            ResourceType NVARCHAR(50) NOT NULL,
            ResourceId NVARCHAR(100) NULL,
            AccessLevel INT NOT NULL,
            GrantedBy UNIQUEIDENTIFIER NOT NULL,
            GrantedDate DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
            ExpiresDate DATETIME2(7) NULL,
            IsActive BIT NOT NULL DEFAULT 1,
            RevokedBy UNIQUEIDENTIFIER NULL,
            RevokedDate DATETIME2(7) NULL,
            RevokeReason NVARCHAR(500) NULL,
            
            CONSTRAINT PK_RolePermissions PRIMARY KEY CLUSTERED (RolePermissionId)
        );
        PRINT 'Table [auth].[RolePermissions] created successfully.';
    END

    -- =============================================
    -- Table: UserPermissions
    -- =============================================
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserPermissions' AND schema_id = SCHEMA_ID('auth'))
    BEGIN
        PRINT 'Creating table [auth].[UserPermissions]...';
        
        CREATE TABLE [auth].[UserPermissions] (
            UserPermissionId INT IDENTITY(1,1) NOT NULL,
            UserId UNIQUEIDENTIFIER NOT NULL,
            PermissionId INT NOT NULL,
            ResourceType NVARCHAR(50) NOT NULL,
            ResourceId NVARCHAR(100) NULL,
            AccessLevel INT NOT NULL,
            IsGrant BIT NOT NULL DEFAULT 1,
            GrantedBy UNIQUEIDENTIFIER NOT NULL,
            GrantedDate DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
            ExpiresDate DATETIME2(7) NULL,
            Reason NVARCHAR(500) NULL,
            IsActive BIT NOT NULL DEFAULT 1,
            RevokedBy UNIQUEIDENTIFIER NULL,
            RevokedDate DATETIME2(7) NULL,
            
            CONSTRAINT PK_UserPermissions PRIMARY KEY CLUSTERED (UserPermissionId)
        );
        PRINT 'Table [auth].[UserPermissions] created successfully.';
    END

    COMMIT TRANSACTION;
    PRINT 'Permission tables transaction committed successfully.';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'ERROR in permission tables creation:';
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS NVARCHAR(10));
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    RETURN;
END CATCH

-- =============================================
-- Add Foreign Key Constraints
-- =============================================
BEGIN TRY
    PRINT 'Adding foreign key constraints...';

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_RolePermissions_Roles')
    BEGIN
        ALTER TABLE [auth].[RolePermissions] ADD CONSTRAINT FK_RolePermissions_Roles 
            FOREIGN KEY (RoleId) REFERENCES [auth].[Roles](RoleId);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_RolePermissions_Permissions')
    BEGIN
        ALTER TABLE [auth].[RolePermissions] ADD CONSTRAINT FK_RolePermissions_Permissions 
            FOREIGN KEY (PermissionId) REFERENCES [auth].[Permissions](PermissionId);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_RolePermissions_GrantedBy')
    BEGIN
        ALTER TABLE [auth].[RolePermissions] ADD CONSTRAINT FK_RolePermissions_GrantedBy 
            FOREIGN KEY (GrantedBy) REFERENCES [auth].[Users](UserId);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserPermissions_Users')
    BEGIN
        ALTER TABLE [auth].[UserPermissions] ADD CONSTRAINT FK_UserPermissions_Users 
            FOREIGN KEY (UserId) REFERENCES [auth].[Users](UserId);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserPermissions_Permissions')
    BEGIN
        ALTER TABLE [auth].[UserPermissions] ADD CONSTRAINT FK_UserPermissions_Permissions 
            FOREIGN KEY (PermissionId) REFERENCES [auth].[Permissions](PermissionId);
    END

    PRINT 'Foreign key constraints added successfully.';

END TRY
BEGIN CATCH
    PRINT 'ERROR adding foreign key constraints:';
    PRINT 'Error Message: ' + ERROR_MESSAGE();
END CATCH

-- =============================================
-- Create Filtered Unique Index (replaces WHERE constraint)
-- =============================================
BEGIN TRY
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_IX_RolePermissions_Active')
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UQ_IX_RolePermissions_Active
        ON [auth].[RolePermissions] (RoleId, PermissionId, ResourceType, ResourceId)
        WHERE IsActive = 1;
        PRINT 'Created filtered unique index UQ_IX_RolePermissions_Active.';
    END

    -- Create other indexes
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RolePermissions_RoleId')
    BEGIN
        CREATE NONCLUSTERED INDEX IX_RolePermissions_RoleId 
        ON [auth].[RolePermissions](RoleId, IsActive) 
        INCLUDE (PermissionId, ResourceType, AccessLevel);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserPermissions_UserId')
    BEGIN
        CREATE NONCLUSTERED INDEX IX_UserPermissions_UserId 
        ON [auth].[UserPermissions](UserId, IsActive) 
        INCLUDE (PermissionId, ResourceType, AccessLevel, IsGrant);
    END

    PRINT 'Permission table indexes created successfully.';

END TRY
BEGIN CATCH
    PRINT 'ERROR creating indexes:';
    PRINT 'Error Message: ' + ERROR_MESSAGE();
END CATCH

PRINT 'Permission and access control tables setup completed successfully.';
GO
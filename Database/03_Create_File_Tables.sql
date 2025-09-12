-- =============================================
-- File Management Tables
-- Schema: docs
-- Re-runnable: YES
-- =============================================

SET NOCOUNT ON;
USE [AuditTrail];
GO

PRINT 'Starting file tables creation...';

BEGIN TRANSACTION;
BEGIN TRY

    -- =============================================
    -- Table: FileCategories
    -- =============================================
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FileCategories' AND schema_id = SCHEMA_ID('docs'))
    BEGIN
        PRINT 'Creating table [docs].[FileCategories]...';
        
        CREATE TABLE [docs].[FileCategories] (
            CategoryId INT IDENTITY(1,1) NOT NULL,
            CategoryName NVARCHAR(100) NOT NULL,
            CategoryPath NVARCHAR(500) NOT NULL,
            ParentCategoryId INT NULL,
            Description NVARCHAR(500) NULL,
            IsActive BIT NOT NULL DEFAULT 1,
            CreatedDate DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
            CreatedBy UNIQUEIDENTIFIER NOT NULL,
            
            CONSTRAINT PK_FileCategories PRIMARY KEY CLUSTERED (CategoryId),
            CONSTRAINT UQ_FileCategories_Path UNIQUE (CategoryPath)
        );
        
        PRINT 'Table [docs].[FileCategories] created successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Table [docs].[FileCategories] already exists. Skipping creation.';
    END

    -- =============================================
    -- Table: Files
    -- =============================================
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Files' AND schema_id = SCHEMA_ID('docs'))
    BEGIN
        PRINT 'Creating table [docs].[Files]...';
        
        CREATE TABLE [docs].[Files] (
            FileId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
            FileName NVARCHAR(255) NOT NULL,
            FileExtension NVARCHAR(10) NOT NULL,
            FilePath NVARCHAR(500) NOT NULL,
            CategoryId INT NULL,
            Version INT NOT NULL DEFAULT 1,
            FileSize BIGINT NOT NULL,
            ContentType NVARCHAR(100) NOT NULL,
            BlobUrl NVARCHAR(1000) NOT NULL,
            S3BucketName NVARCHAR(255) NOT NULL,
            S3Key NVARCHAR(1000) NOT NULL,
            Checksum NVARCHAR(64) NOT NULL,
            ChecksumAlgorithm NVARCHAR(20) NOT NULL DEFAULT 'SHA256',
            OriginalFileName NVARCHAR(255) NOT NULL,
            StudyId NVARCHAR(100) NULL,
            DocumentType NVARCHAR(100) NULL,
            Tags NVARCHAR(500) NULL,
            Description NVARCHAR(MAX) NULL,
            IsEncrypted BIT NOT NULL DEFAULT 1,
            EncryptionKeyId NVARCHAR(100) NULL,
            UploadedBy UNIQUEIDENTIFIER NOT NULL,
            UploadedDate DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
            IsDeleted BIT NOT NULL DEFAULT 0,
            DeletedDate DATETIME2(7) NULL,
            DeletedBy UNIQUEIDENTIFIER NULL,
            DeleteReason NVARCHAR(500) NULL,
            IsArchived BIT NOT NULL DEFAULT 0,
            ArchivedDate DATETIME2(7) NULL,
            ArchivedBy UNIQUEIDENTIFIER NULL,
            ArchiveStorageTier NVARCHAR(50) NULL,
            
            CONSTRAINT PK_Files PRIMARY KEY CLUSTERED (FileId),
            CONSTRAINT UQ_Files_Path_Name_Version UNIQUE (FilePath, FileName, Version),
            CONSTRAINT CHK_Files_Extension CHECK (FileExtension IN ('.pdf', '.doc', '.docx')),
            CONSTRAINT CHK_Files_Size CHECK (FileSize > 0 AND FileSize <= 52428800)
        );
        
        PRINT 'Table [docs].[Files] created successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Table [docs].[Files] already exists. Skipping creation.';
    END

    -- =============================================
    -- Table: FileVersions
    -- =============================================
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FileVersions' AND schema_id = SCHEMA_ID('docs'))
    BEGIN
        PRINT 'Creating table [docs].[FileVersions]...';
        
        CREATE TABLE [docs].[FileVersions] (
            VersionId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
            FileId UNIQUEIDENTIFIER NOT NULL,
            Version INT NOT NULL,
            FileName NVARCHAR(255) NOT NULL,
            FileSize BIGINT NOT NULL,
            BlobUrl NVARCHAR(1000) NOT NULL,
            S3Key NVARCHAR(1000) NOT NULL,
            Checksum NVARCHAR(64) NOT NULL,
            VersionComment NVARCHAR(500) NULL,
            CreatedBy UNIQUEIDENTIFIER NOT NULL,
            CreatedDate DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
            IsActive BIT NOT NULL DEFAULT 1,
            
            CONSTRAINT PK_FileVersions PRIMARY KEY CLUSTERED (VersionId),
            CONSTRAINT UQ_FileVersions_FileId_Version UNIQUE (FileId, Version)
        );
        
        PRINT 'Table [docs].[FileVersions] created successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Table [docs].[FileVersions] already exists. Skipping creation.';
    END

    -- =============================================
    -- Table: FileMetadata
    -- =============================================
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FileMetadata' AND schema_id = SCHEMA_ID('docs'))
    BEGIN
        PRINT 'Creating table [docs].[FileMetadata]...';
        
        CREATE TABLE [docs].[FileMetadata] (
            MetadataId INT IDENTITY(1,1) NOT NULL,
            FileId UNIQUEIDENTIFIER NOT NULL,
            MetadataKey NVARCHAR(100) NOT NULL,
            MetadataValue NVARCHAR(MAX) NOT NULL,
            DataType NVARCHAR(50) NOT NULL DEFAULT 'String',
            CreatedDate DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
            CreatedBy UNIQUEIDENTIFIER NOT NULL,
            
            CONSTRAINT PK_FileMetadata PRIMARY KEY CLUSTERED (MetadataId),
            CONSTRAINT UQ_FileMetadata_FileId_Key UNIQUE (FileId, MetadataKey)
        );
        
        PRINT 'Table [docs].[FileMetadata] created successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Table [docs].[FileMetadata] already exists. Skipping creation.';
    END

    -- =============================================
    -- Table: FileAccess
    -- =============================================
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FileAccess' AND schema_id = SCHEMA_ID('docs'))
    BEGIN
        PRINT 'Creating table [docs].[FileAccess]...';
        
        CREATE TABLE [docs].[FileAccess] (
            AccessId INT IDENTITY(1,1) NOT NULL,
            FileId UNIQUEIDENTIFIER NOT NULL,
            UserId UNIQUEIDENTIFIER NULL,
            RoleId INT NULL,
            AccessLevel NVARCHAR(50) NOT NULL,
            GrantedBy UNIQUEIDENTIFIER NOT NULL,
            GrantedDate DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
            ExpiryDate DATETIME2(7) NULL,
            IsActive BIT NOT NULL DEFAULT 1,
            RevokedBy UNIQUEIDENTIFIER NULL,
            RevokedDate DATETIME2(7) NULL,
            RevokeReason NVARCHAR(500) NULL,
            
            CONSTRAINT PK_FileAccess PRIMARY KEY CLUSTERED (AccessId),
            CONSTRAINT CHK_FileAccess_UserOrRole CHECK ((UserId IS NOT NULL AND RoleId IS NULL) OR (UserId IS NULL AND RoleId IS NOT NULL))
        );
        
        PRINT 'Table [docs].[FileAccess] created successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Table [docs].[FileAccess] already exists. Skipping creation.';
    END

    -- =============================================
    -- Table: FileLocks
    -- =============================================
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FileLocks' AND schema_id = SCHEMA_ID('docs'))
    BEGIN
        PRINT 'Creating table [docs].[FileLocks]...';
        
        CREATE TABLE [docs].[FileLocks] (
            LockId INT IDENTITY(1,1) NOT NULL,
            FileId UNIQUEIDENTIFIER NOT NULL,
            LockedBy UNIQUEIDENTIFIER NOT NULL,
            LockType NVARCHAR(50) NOT NULL,
            LockedDate DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
            ExpiryDate DATETIME2(7) NOT NULL,
            IsActive BIT NOT NULL DEFAULT 1,
            ReleasedDate DATETIME2(7) NULL,
            
            CONSTRAINT PK_FileLocks PRIMARY KEY CLUSTERED (LockId)
        );
        
        PRINT 'Table [docs].[FileLocks] created successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Table [docs].[FileLocks] already exists. Skipping creation.';
    END

    COMMIT TRANSACTION;
    PRINT 'File tables transaction committed successfully.';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'ERROR in file tables creation:';
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS NVARCHAR(10));
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Line Number: ' + CAST(ERROR_LINE() AS NVARCHAR(10));
    RETURN;
END CATCH

-- =============================================
-- Add Foreign Key Constraints (safe to run multiple times)
-- =============================================
BEGIN TRY
    PRINT 'Adding foreign key constraints...';

    -- FileCategories self-referencing FK
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FileCategories_Parent')
    BEGIN
        ALTER TABLE [docs].[FileCategories] ADD CONSTRAINT FK_FileCategories_Parent 
            FOREIGN KEY (ParentCategoryId) REFERENCES [docs].[FileCategories](CategoryId);
        PRINT 'Added FK_FileCategories_Parent constraint.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FileCategories_CreatedBy')
    BEGIN
        ALTER TABLE [docs].[FileCategories] ADD CONSTRAINT FK_FileCategories_CreatedBy 
            FOREIGN KEY (CreatedBy) REFERENCES [auth].[Users](UserId);
        PRINT 'Added FK_FileCategories_CreatedBy constraint.';
    END

    -- Files table FKs
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Files_Category')
    BEGIN
        ALTER TABLE [docs].[Files] ADD CONSTRAINT FK_Files_Category 
            FOREIGN KEY (CategoryId) REFERENCES [docs].[FileCategories](CategoryId);
        PRINT 'Added FK_Files_Category constraint.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Files_UploadedBy')
    BEGIN
        ALTER TABLE [docs].[Files] ADD CONSTRAINT FK_Files_UploadedBy 
            FOREIGN KEY (UploadedBy) REFERENCES [auth].[Users](UserId);
        PRINT 'Added FK_Files_UploadedBy constraint.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Files_DeletedBy')
    BEGIN
        ALTER TABLE [docs].[Files] ADD CONSTRAINT FK_Files_DeletedBy 
            FOREIGN KEY (DeletedBy) REFERENCES [auth].[Users](UserId);
        PRINT 'Added FK_Files_DeletedBy constraint.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Files_ArchivedBy')
    BEGIN
        ALTER TABLE [docs].[Files] ADD CONSTRAINT FK_Files_ArchivedBy 
            FOREIGN KEY (ArchivedBy) REFERENCES [auth].[Users](UserId);
        PRINT 'Added FK_Files_ArchivedBy constraint.';
    END

    -- FileVersions table FKs
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FileVersions_Files')
    BEGIN
        ALTER TABLE [docs].[FileVersions] ADD CONSTRAINT FK_FileVersions_Files 
            FOREIGN KEY (FileId) REFERENCES [docs].[Files](FileId);
        PRINT 'Added FK_FileVersions_Files constraint.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FileVersions_CreatedBy')
    BEGIN
        ALTER TABLE [docs].[FileVersions] ADD CONSTRAINT FK_FileVersions_CreatedBy 
            FOREIGN KEY (CreatedBy) REFERENCES [auth].[Users](UserId);
        PRINT 'Added FK_FileVersions_CreatedBy constraint.';
    END

    -- FileMetadata table FKs
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FileMetadata_Files')
    BEGIN
        ALTER TABLE [docs].[FileMetadata] ADD CONSTRAINT FK_FileMetadata_Files 
            FOREIGN KEY (FileId) REFERENCES [docs].[Files](FileId);
        PRINT 'Added FK_FileMetadata_Files constraint.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FileMetadata_CreatedBy')
    BEGIN
        ALTER TABLE [docs].[FileMetadata] ADD CONSTRAINT FK_FileMetadata_CreatedBy 
            FOREIGN KEY (CreatedBy) REFERENCES [auth].[Users](UserId);
        PRINT 'Added FK_FileMetadata_CreatedBy constraint.';
    END

    -- FileAccess table FKs
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FileAccess_Files')
    BEGIN
        ALTER TABLE [docs].[FileAccess] ADD CONSTRAINT FK_FileAccess_Files 
            FOREIGN KEY (FileId) REFERENCES [docs].[Files](FileId);
        PRINT 'Added FK_FileAccess_Files constraint.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FileAccess_Users')
    BEGIN
        ALTER TABLE [docs].[FileAccess] ADD CONSTRAINT FK_FileAccess_Users 
            FOREIGN KEY (UserId) REFERENCES [auth].[Users](UserId);
        PRINT 'Added FK_FileAccess_Users constraint.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FileAccess_Roles')
    BEGIN
        ALTER TABLE [docs].[FileAccess] ADD CONSTRAINT FK_FileAccess_Roles 
            FOREIGN KEY (RoleId) REFERENCES [auth].[Roles](RoleId);
        PRINT 'Added FK_FileAccess_Roles constraint.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FileAccess_GrantedBy')
    BEGIN
        ALTER TABLE [docs].[FileAccess] ADD CONSTRAINT FK_FileAccess_GrantedBy 
            FOREIGN KEY (GrantedBy) REFERENCES [auth].[Users](UserId);
        PRINT 'Added FK_FileAccess_GrantedBy constraint.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FileAccess_RevokedBy')
    BEGIN
        ALTER TABLE [docs].[FileAccess] ADD CONSTRAINT FK_FileAccess_RevokedBy 
            FOREIGN KEY (RevokedBy) REFERENCES [auth].[Users](UserId);
        PRINT 'Added FK_FileAccess_RevokedBy constraint.';
    END

    -- FileLocks table FKs
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FileLocks_Files')
    BEGIN
        ALTER TABLE [docs].[FileLocks] ADD CONSTRAINT FK_FileLocks_Files 
            FOREIGN KEY (FileId) REFERENCES [docs].[Files](FileId);
        PRINT 'Added FK_FileLocks_Files constraint.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FileLocks_LockedBy')
    BEGIN
        ALTER TABLE [docs].[FileLocks] ADD CONSTRAINT FK_FileLocks_LockedBy 
            FOREIGN KEY (LockedBy) REFERENCES [auth].[Users](UserId);
        PRINT 'Added FK_FileLocks_LockedBy constraint.';
    END

    PRINT 'Foreign key constraints added successfully.';

END TRY
BEGIN CATCH
    PRINT 'ERROR adding foreign key constraints:';
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS NVARCHAR(10));
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    -- Continue anyway
END CATCH

-- =============================================
-- Create Indexes (safe to run multiple times)
-- =============================================
BEGIN TRY
    PRINT 'Creating indexes for file tables...';

    -- Files table indexes
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Files_UploadedDate' AND object_id = OBJECT_ID('[docs].[Files]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Files_UploadedDate 
        ON [docs].[Files](UploadedDate DESC) 
        INCLUDE (FileName, FilePath, FileSize, UploadedBy)
        WHERE IsDeleted = 0;
        PRINT 'Created index IX_Files_UploadedDate.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Files_UploadedBy' AND object_id = OBJECT_ID('[docs].[Files]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Files_UploadedBy 
        ON [docs].[Files](UploadedBy, UploadedDate DESC) 
        INCLUDE (FileName, FilePath, Version)
        WHERE IsDeleted = 0;
        PRINT 'Created index IX_Files_UploadedBy.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Files_StudyId' AND object_id = OBJECT_ID('[docs].[Files]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Files_StudyId 
        ON [docs].[Files](StudyId) 
        INCLUDE (FileName, DocumentType, Version)
        WHERE IsDeleted = 0 AND StudyId IS NOT NULL;
        PRINT 'Created index IX_Files_StudyId.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Files_FilePath' AND object_id = OBJECT_ID('[docs].[Files]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Files_FilePath 
        ON [docs].[Files](FilePath) 
        INCLUDE (FileName, Version, FileSize)
        WHERE IsDeleted = 0;
        PRINT 'Created index IX_Files_FilePath.';
    END

    -- FileVersions table indexes
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FileVersions_FileId' AND object_id = OBJECT_ID('[docs].[FileVersions]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_FileVersions_FileId 
        ON [docs].[FileVersions](FileId, Version DESC) 
        INCLUDE (FileName, FileSize, CreatedDate);
        PRINT 'Created index IX_FileVersions_FileId.';
    END

    -- FileAccess table indexes
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FileAccess_FileId' AND object_id = OBJECT_ID('[docs].[FileAccess]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_FileAccess_FileId 
        ON [docs].[FileAccess](FileId, IsActive) 
        INCLUDE (UserId, RoleId, AccessLevel);
        PRINT 'Created index IX_FileAccess_FileId.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FileAccess_UserId' AND object_id = OBJECT_ID('[docs].[FileAccess]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_FileAccess_UserId 
        ON [docs].[FileAccess](UserId, IsActive) 
        INCLUDE (FileId, AccessLevel)
        WHERE UserId IS NOT NULL;
        PRINT 'Created index IX_FileAccess_UserId.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FileAccess_RoleId' AND object_id = OBJECT_ID('[docs].[FileAccess]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_FileAccess_RoleId 
        ON [docs].[FileAccess](RoleId, IsActive) 
        INCLUDE (FileId, AccessLevel)
        WHERE RoleId IS NOT NULL;
        PRINT 'Created index IX_FileAccess_RoleId.';
    END

    -- FileLocks table indexes
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FileLocks_FileId_Active' AND object_id = OBJECT_ID('[docs].[FileLocks]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_FileLocks_FileId_Active 
        ON [docs].[FileLocks](FileId, IsActive) 
        INCLUDE (LockedBy, LockType, ExpiryDate)
        WHERE IsActive = 1;
        PRINT 'Created index IX_FileLocks_FileId_Active.';
    END

    PRINT 'File table indexes created/verified successfully.';

END TRY
BEGIN CATCH
    PRINT 'ERROR creating indexes:';
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS NVARCHAR(10));
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    -- Continue anyway
END CATCH

PRINT 'File management tables setup completed successfully.';
GO
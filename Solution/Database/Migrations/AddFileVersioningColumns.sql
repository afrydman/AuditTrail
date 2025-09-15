-- Migration: Add File Versioning Columns
-- Description: Adds IsCurrentVersion and ParentFileId columns to support file versioning
-- Date: 2025-09-15
-- Version: 1.0

USE [AuditTrail];
GO

BEGIN TRANSACTION;

BEGIN TRY
    -- Check if the columns already exist
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                   WHERE TABLE_SCHEMA = 'docs' AND TABLE_NAME = 'Files' AND COLUMN_NAME = 'IsCurrentVersion')
    BEGIN
        ALTER TABLE [docs].[Files] 
        ADD [IsCurrentVersion] bit NOT NULL DEFAULT(1);
        PRINT 'Added IsCurrentVersion column to docs.Files table';
    END
    ELSE
    BEGIN
        PRINT 'IsCurrentVersion column already exists in docs.Files table';
    END

    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                   WHERE TABLE_SCHEMA = 'docs' AND TABLE_NAME = 'Files' AND COLUMN_NAME = 'ParentFileId')
    BEGIN
        ALTER TABLE [docs].[Files] 
        ADD [ParentFileId] uniqueidentifier NULL;
        PRINT 'Added ParentFileId column to docs.Files table';
    END
    ELSE
    BEGIN
        PRINT 'ParentFileId column already exists in docs.Files table';
    END

    -- Update all existing files to be current versions (only if column was just added)
    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_SCHEMA = 'docs' AND TABLE_NAME = 'Files' AND COLUMN_NAME = 'IsCurrentVersion')
    BEGIN
        UPDATE [docs].[Files] 
        SET [IsCurrentVersion] = 1 
        WHERE [IsCurrentVersion] IS NULL OR [IsCurrentVersion] = 0;
        PRINT 'Updated existing files to be current versions';
    END

    -- Add foreign key constraint for ParentFileId if it doesn't exist
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS 
                   WHERE CONSTRAINT_NAME = 'FK_Files_ParentFile')
    BEGIN
        ALTER TABLE [docs].[Files]
        ADD CONSTRAINT FK_Files_ParentFile 
        FOREIGN KEY (ParentFileId) REFERENCES [docs].[Files](FileId);
        PRINT 'Added foreign key constraint for ParentFileId';
    END
    ELSE
    BEGIN
        PRINT 'Foreign key constraint for ParentFileId already exists';
    END

    -- Create index on IsCurrentVersion for better query performance
    IF NOT EXISTS (SELECT * FROM sys.indexes 
                   WHERE name = 'IX_Files_IsCurrentVersion' AND object_id = OBJECT_ID('[docs].[Files]'))
    BEGIN
        CREATE INDEX IX_Files_IsCurrentVersion 
        ON [docs].[Files] (IsCurrentVersion, CategoryId) 
        INCLUDE (FileName, Version);
        PRINT 'Created index on IsCurrentVersion';
    END
    ELSE
    BEGIN
        PRINT 'Index on IsCurrentVersion already exists';
    END

    -- Create index on ParentFileId for version queries
    IF NOT EXISTS (SELECT * FROM sys.indexes 
                   WHERE name = 'IX_Files_ParentFileId' AND object_id = OBJECT_ID('[docs].[Files]'))
    BEGIN
        CREATE INDEX IX_Files_ParentFileId 
        ON [docs].[Files] (ParentFileId) 
        WHERE ParentFileId IS NOT NULL;
        PRINT 'Created index on ParentFileId';
    END
    ELSE
    BEGIN
        PRINT 'Index on ParentFileId already exists';
    END

    COMMIT TRANSACTION;
    PRINT 'File versioning columns migration completed successfully';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    
    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();
    
    PRINT 'Error during migration: ' + @ErrorMessage;
    RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
    
END CATCH;
GO
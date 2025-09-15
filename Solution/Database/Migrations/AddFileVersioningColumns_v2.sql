-- Migration: Add File Versioning Columns (Version 2)
-- Description: Adds IsCurrentVersion and ParentFileId columns to support file versioning
-- Date: 2025-09-15

USE [AuditTrail];
GO

-- Add IsCurrentVersion column
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
GO

-- Add ParentFileId column
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
GO

-- Update existing files to be current versions
UPDATE [docs].[Files] 
SET [IsCurrentVersion] = 1 
WHERE [IsCurrentVersion] = 0 OR [IsCurrentVersion] IS NULL;
PRINT 'Updated existing files to be current versions';
GO

-- Add foreign key constraint
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS 
               WHERE CONSTRAINT_NAME = 'FK_Files_ParentFile')
BEGIN
    ALTER TABLE [docs].[Files]
    ADD CONSTRAINT FK_Files_ParentFile 
    FOREIGN KEY (ParentFileId) REFERENCES [docs].[Files](FileId);
    PRINT 'Added foreign key constraint for ParentFileId';
END
GO

-- Create indexes
IF NOT EXISTS (SELECT * FROM sys.indexes 
               WHERE name = 'IX_Files_IsCurrentVersion' AND object_id = OBJECT_ID('[docs].[Files]'))
BEGIN
    CREATE INDEX IX_Files_IsCurrentVersion 
    ON [docs].[Files] (IsCurrentVersion, CategoryId) 
    INCLUDE (FileName, Version);
    PRINT 'Created index on IsCurrentVersion';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes 
               WHERE name = 'IX_Files_ParentFileId' AND object_id = OBJECT_ID('[docs].[Files]'))
BEGIN
    CREATE INDEX IX_Files_ParentFileId 
    ON [docs].[Files] (ParentFileId) 
    WHERE ParentFileId IS NOT NULL;
    PRINT 'Created index on ParentFileId';
END
GO

PRINT 'File versioning migration completed successfully';
GO
USE [AuditTrail];
SET QUOTED_IDENTIFIER ON;
GO

-- Add IsCurrentVersion column if it doesn't exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_SCHEMA = 'docs' AND TABLE_NAME = 'Files' AND COLUMN_NAME = 'IsCurrentVersion')
BEGIN
    ALTER TABLE [docs].[Files] 
    ADD [IsCurrentVersion] bit NOT NULL CONSTRAINT DF_Files_IsCurrentVersion DEFAULT 1;
    PRINT 'Added IsCurrentVersion column to docs.Files table';
END
ELSE
BEGIN
    PRINT 'IsCurrentVersion column already exists';
END
GO

-- Update any null values to 1
UPDATE [docs].[Files] SET [IsCurrentVersion] = 1 WHERE [IsCurrentVersion] IS NULL;
PRINT 'Updated IsCurrentVersion values';
GO

-- Create index
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Files_IsCurrentVersion')
BEGIN
    CREATE INDEX IX_Files_IsCurrentVersion ON [docs].[Files] ([IsCurrentVersion], [CategoryId]);
    PRINT 'Created index on IsCurrentVersion';
END
GO
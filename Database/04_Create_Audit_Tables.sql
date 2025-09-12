-- =============================================
-- Audit Trail Tables (CFR 21 Part 11 Compliant)
-- Schema: audit
-- IMPORTANT: These tables are immutable once created
-- Re-runnable: YES
-- =============================================

SET NOCOUNT ON;
USE [AuditTrail];
GO

PRINT 'Starting audit tables creation...';

BEGIN TRANSACTION;
BEGIN TRY

    -- =============================================
    -- Table: AuditEventTypes
    -- =============================================
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AuditEventTypes' AND schema_id = SCHEMA_ID('audit'))
    BEGIN
        PRINT 'Creating table [audit].[AuditEventTypes]...';
        
        CREATE TABLE [audit].[AuditEventTypes] (
            EventTypeId INT IDENTITY(1,1) NOT NULL,
            EventCategory NVARCHAR(50) NOT NULL,
            EventType NVARCHAR(100) NOT NULL,
            EventDescription NVARCHAR(500) NULL,
            Severity NVARCHAR(20) NOT NULL DEFAULT 'Info',
            IsActive BIT NOT NULL DEFAULT 1,
            
            CONSTRAINT PK_AuditEventTypes PRIMARY KEY CLUSTERED (EventTypeId),
            CONSTRAINT UQ_AuditEventTypes_EventType UNIQUE (EventType)
        );
        
        PRINT 'Table [audit].[AuditEventTypes] created successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Table [audit].[AuditEventTypes] already exists. Skipping creation.';
    END

    -- =============================================
    -- Table: AuditTrail (IMMUTABLE)
    -- =============================================
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AuditTrail' AND schema_id = SCHEMA_ID('audit'))
    BEGIN
        PRINT 'Creating table [audit].[AuditTrail] (IMMUTABLE)...';
        
        CREATE TABLE [audit].[AuditTrail] (
            AuditId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
            EventType NVARCHAR(100) NOT NULL,
            EventCategory NVARCHAR(50) NOT NULL,
            Timestamp DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
            UserId UNIQUEIDENTIFIER NULL,
            Username NVARCHAR(100) NULL,
            RoleName NVARCHAR(50) NULL,
            IPAddress NVARCHAR(45) NULL,
            UserAgent NVARCHAR(500) NULL,
            SessionId UNIQUEIDENTIFIER NULL,
            EntityType NVARCHAR(100) NULL,
            EntityId NVARCHAR(100) NULL,
            EntityName NVARCHAR(255) NULL,
            Action NVARCHAR(100) NOT NULL,
            OldValue NVARCHAR(MAX) NULL,
            NewValue NVARCHAR(MAX) NULL,
            AdditionalData NVARCHAR(MAX) NULL,
            Result NVARCHAR(50) NOT NULL DEFAULT 'Success',
            ErrorMessage NVARCHAR(MAX) NULL,
            Duration INT NULL,
            ServerName NVARCHAR(100) NULL,
            ApplicationVersion NVARCHAR(50) NULL,
            
            CONSTRAINT PK_AuditTrail PRIMARY KEY CLUSTERED (AuditId)
        );
        
        PRINT 'Table [audit].[AuditTrail] created successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Table [audit].[AuditTrail] already exists. Skipping creation.';
    END

    -- =============================================
    -- Table: FileAuditTrail
    -- =============================================
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FileAuditTrail' AND schema_id = SCHEMA_ID('audit'))
    BEGIN
        PRINT 'Creating table [audit].[FileAuditTrail]...';
        
        CREATE TABLE [audit].[FileAuditTrail] (
            FileAuditId BIGINT IDENTITY(1,1) NOT NULL,
            AuditId UNIQUEIDENTIFIER NOT NULL,
            FileId UNIQUEIDENTIFIER NOT NULL,
            FileName NVARCHAR(255) NOT NULL,
            FilePath NVARCHAR(500) NOT NULL,
            FileVersion INT NULL,
            Operation NVARCHAR(50) NOT NULL,
            OperationDetails NVARCHAR(MAX) NULL,
            FileSize BIGINT NULL,
            Checksum NVARCHAR(64) NULL,
            AccessGranted BIT NOT NULL DEFAULT 1,
            DenialReason NVARCHAR(500) NULL,
            
            CONSTRAINT PK_FileAuditTrail PRIMARY KEY CLUSTERED (FileAuditId)
        );
        
        PRINT 'Table [audit].[FileAuditTrail] created successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Table [audit].[FileAuditTrail] already exists. Skipping creation.';
    END

    -- =============================================
    -- Table: AuditTrailArchive
    -- =============================================
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AuditTrailArchive' AND schema_id = SCHEMA_ID('audit'))
    BEGIN
        PRINT 'Creating table [audit].[AuditTrailArchive]...';
        
        CREATE TABLE [audit].[AuditTrailArchive] (
            AuditId UNIQUEIDENTIFIER NOT NULL,
            EventType NVARCHAR(100) NOT NULL,
            EventCategory NVARCHAR(50) NOT NULL,
            Timestamp DATETIME2(7) NOT NULL,
            UserId UNIQUEIDENTIFIER NULL,
            Username NVARCHAR(100) NULL,
            RoleName NVARCHAR(50) NULL,
            IPAddress NVARCHAR(45) NULL,
            UserAgent NVARCHAR(500) NULL,
            SessionId UNIQUEIDENTIFIER NULL,
            EntityType NVARCHAR(100) NULL,
            EntityId NVARCHAR(100) NULL,
            EntityName NVARCHAR(255) NULL,
            Action NVARCHAR(100) NOT NULL,
            OldValue NVARCHAR(MAX) NULL,
            NewValue NVARCHAR(MAX) NULL,
            AdditionalData NVARCHAR(MAX) NULL,
            Result NVARCHAR(50) NOT NULL,
            ErrorMessage NVARCHAR(MAX) NULL,
            Duration INT NULL,
            ServerName NVARCHAR(100) NULL,
            ApplicationVersion NVARCHAR(50) NULL,
            ArchivedDate DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
            
            CONSTRAINT PK_AuditTrailArchive PRIMARY KEY CLUSTERED (AuditId)
        ) WITH (DATA_COMPRESSION = PAGE);
        
        PRINT 'Table [audit].[AuditTrailArchive] created successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Table [audit].[AuditTrailArchive] already exists. Skipping creation.';
    END

    -- =============================================
    -- Table: AuditSummary
    -- =============================================
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AuditSummary' AND schema_id = SCHEMA_ID('audit'))
    BEGIN
        PRINT 'Creating table [audit].[AuditSummary]...';
        
        CREATE TABLE [audit].[AuditSummary] (
            SummaryId INT IDENTITY(1,1) NOT NULL,
            SummaryDate DATE NOT NULL,
            EventCategory NVARCHAR(50) NOT NULL,
            EventType NVARCHAR(100) NOT NULL,
            TotalCount INT NOT NULL,
            SuccessCount INT NOT NULL,
            FailureCount INT NOT NULL,
            UniqueUsers INT NOT NULL,
            CreatedDate DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
            
            CONSTRAINT PK_AuditSummary PRIMARY KEY CLUSTERED (SummaryId),
            CONSTRAINT UQ_AuditSummary_Date_Category_Type UNIQUE (SummaryDate, EventCategory, EventType)
        );
        
        PRINT 'Table [audit].[AuditSummary] created successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Table [audit].[AuditSummary] already exists. Skipping creation.';
    END

    COMMIT TRANSACTION;
    PRINT 'Audit tables transaction committed successfully.';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'ERROR in audit tables creation:';
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

    -- FileAuditTrail FK
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FileAuditTrail_AuditTrail')
    BEGIN
        ALTER TABLE [audit].[FileAuditTrail] ADD CONSTRAINT FK_FileAuditTrail_AuditTrail 
            FOREIGN KEY (AuditId) REFERENCES [audit].[AuditTrail](AuditId);
        PRINT 'Added FK_FileAuditTrail_AuditTrail constraint.';
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
-- Create IMMUTABILITY TRIGGERS (Critical for Compliance)
-- =============================================
BEGIN TRY
    PRINT 'Creating immutability triggers...';

    -- Check if trigger exists and drop if it does
    IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TR_AuditTrail_PreventModification')
    BEGIN
        DROP TRIGGER [audit].[TR_AuditTrail_PreventModification];
        PRINT 'Dropped existing trigger TR_AuditTrail_PreventModification.';
    END

    -- Create the immutability trigger
    EXEC('
    CREATE TRIGGER [audit].[TR_AuditTrail_PreventModification]
    ON [audit].[AuditTrail]
    INSTEAD OF UPDATE, DELETE
    AS
    BEGIN
        SET NOCOUNT ON;
        
        DECLARE @UserId NVARCHAR(100) = SYSTEM_USER;
        DECLARE @ErrorMsg NVARCHAR(500) = ''Audit trail records cannot be modified or deleted. Attempted by: '' + @UserId;
        
        RAISERROR(@ErrorMsg, 16, 1);
        ROLLBACK TRANSACTION;
    END
    ');
    PRINT 'Created trigger TR_AuditTrail_PreventModification for audit trail immutability.';

    -- Check if FileAuditTrail trigger exists and drop if it does
    IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TR_FileAuditTrail_PreventModification')
    BEGIN
        DROP TRIGGER [audit].[TR_FileAuditTrail_PreventModification];
        PRINT 'Dropped existing trigger TR_FileAuditTrail_PreventModification.';
    END

    -- Create the FileAuditTrail immutability trigger
    EXEC('
    CREATE TRIGGER [audit].[TR_FileAuditTrail_PreventModification]
    ON [audit].[FileAuditTrail]
    INSTEAD OF UPDATE, DELETE
    AS
    BEGIN
        SET NOCOUNT ON;
        RAISERROR(''File audit trail records cannot be modified or deleted'', 16, 1);
        ROLLBACK TRANSACTION;
    END
    ');
    PRINT 'Created trigger TR_FileAuditTrail_PreventModification for file audit trail immutability.';

END TRY
BEGIN CATCH
    PRINT 'ERROR creating triggers:';
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS NVARCHAR(10));
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'CRITICAL: Immutability triggers are required for compliance!';
    RETURN;
END CATCH

-- =============================================
-- Create Indexes (safe to run multiple times)
-- =============================================
BEGIN TRY
    PRINT 'Creating indexes for audit tables...';

    -- AuditTrail indexes
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditTrail_Timestamp' AND object_id = OBJECT_ID('[audit].[AuditTrail]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_AuditTrail_Timestamp 
        ON [audit].[AuditTrail](Timestamp DESC) 
        INCLUDE (EventType, UserId, Username, EntityType, EntityId, Result);
        PRINT 'Created index IX_AuditTrail_Timestamp.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditTrail_UserId' AND object_id = OBJECT_ID('[audit].[AuditTrail]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_AuditTrail_UserId 
        ON [audit].[AuditTrail](UserId, Timestamp DESC) 
        INCLUDE (EventType, Action, Result)
        WHERE UserId IS NOT NULL;
        PRINT 'Created index IX_AuditTrail_UserId.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditTrail_EntityId' AND object_id = OBJECT_ID('[audit].[AuditTrail]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_AuditTrail_EntityId 
        ON [audit].[AuditTrail](EntityType, EntityId, Timestamp DESC) 
        INCLUDE (EventType, Action, UserId, Username)
        WHERE EntityId IS NOT NULL;
        PRINT 'Created index IX_AuditTrail_EntityId.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditTrail_EventType' AND object_id = OBJECT_ID('[audit].[AuditTrail]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_AuditTrail_EventType 
        ON [audit].[AuditTrail](EventType, Timestamp DESC) 
        INCLUDE (UserId, Username, Result);
        PRINT 'Created index IX_AuditTrail_EventType.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditTrail_IPAddress' AND object_id = OBJECT_ID('[audit].[AuditTrail]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_AuditTrail_IPAddress 
        ON [audit].[AuditTrail](IPAddress, Timestamp DESC) 
        INCLUDE (UserId, Username, EventType)
        WHERE IPAddress IS NOT NULL;
        PRINT 'Created index IX_AuditTrail_IPAddress.';
    END

    -- FileAuditTrail indexes
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FileAuditTrail_FileId' AND object_id = OBJECT_ID('[audit].[FileAuditTrail]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_FileAuditTrail_FileId 
        ON [audit].[FileAuditTrail](FileId, FileAuditId DESC) 
        INCLUDE (Operation, FileName, FilePath);
        PRINT 'Created index IX_FileAuditTrail_FileId.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FileAuditTrail_AuditId' AND object_id = OBJECT_ID('[audit].[FileAuditTrail]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_FileAuditTrail_AuditId 
        ON [audit].[FileAuditTrail](AuditId);
        PRINT 'Created index IX_FileAuditTrail_AuditId.';
    END

    -- AuditSummary indexes
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditSummary_Date' AND object_id = OBJECT_ID('[audit].[AuditSummary]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_AuditSummary_Date 
        ON [audit].[AuditSummary](SummaryDate DESC, EventCategory) 
        INCLUDE (TotalCount, SuccessCount, FailureCount);
        PRINT 'Created index IX_AuditSummary_Date.';
    END

    PRINT 'Audit table indexes created/verified successfully.';

END TRY
BEGIN CATCH
    PRINT 'ERROR creating indexes:';
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS NVARCHAR(10));
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    -- Continue anyway
END CATCH

-- =============================================
-- Create Partition Function and Scheme (optional but recommended)
-- =============================================
BEGIN TRY
    PRINT 'Creating partition function and scheme...';

    -- Check if partition function exists
    IF NOT EXISTS (SELECT 1 FROM sys.partition_functions WHERE name = 'PF_AuditTrail_Monthly')
    BEGIN
        CREATE PARTITION FUNCTION PF_AuditTrail_Monthly (DATETIME2(7))
        AS RANGE RIGHT FOR VALUES (
            '2024-01-01T00:00:00',
            '2024-02-01T00:00:00',
            '2024-03-01T00:00:00',
            '2024-04-01T00:00:00',
            '2024-05-01T00:00:00',
            '2024-06-01T00:00:00',
            '2024-07-01T00:00:00',
            '2024-08-01T00:00:00',
            '2024-09-01T00:00:00',
            '2024-10-01T00:00:00',
            '2024-11-01T00:00:00',
            '2024-12-01T00:00:00',
            '2025-01-01T00:00:00',
            '2025-02-01T00:00:00',
            '2025-03-01T00:00:00',
            '2025-04-01T00:00:00',
            '2025-05-01T00:00:00',
            '2025-06-01T00:00:00',
            '2025-07-01T00:00:00',
            '2025-08-01T00:00:00',
            '2025-09-01T00:00:00',
            '2025-10-01T00:00:00',
            '2025-11-01T00:00:00',
            '2025-12-01T00:00:00'
        );
        PRINT 'Created partition function PF_AuditTrail_Monthly.';
    END
    ELSE
    BEGIN
        PRINT 'Partition function PF_AuditTrail_Monthly already exists.';
    END

    -- Check if partition scheme exists
    IF NOT EXISTS (SELECT 1 FROM sys.partition_schemes WHERE name = 'PS_AuditTrail_Monthly')
    BEGIN
        CREATE PARTITION SCHEME PS_AuditTrail_Monthly
        AS PARTITION PF_AuditTrail_Monthly
        ALL TO ([PRIMARY]);
        PRINT 'Created partition scheme PS_AuditTrail_Monthly.';
    END
    ELSE
    BEGIN
        PRINT 'Partition scheme PS_AuditTrail_Monthly already exists.';
    END

END TRY
BEGIN CATCH
    PRINT 'WARNING: Error creating partition function/scheme:';
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS NVARCHAR(10));
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Partitioning is optional - continuing without it.';
END CATCH

PRINT 'Audit trail tables setup completed successfully.';
PRINT 'IMPORTANT: Immutability triggers are active - audit records cannot be modified or deleted.';
GO
-- =============================================
-- Configuration and Retention Policy Tables
-- Schema: config
-- Re-runnable: YES
-- =============================================

SET NOCOUNT ON;
USE [AuditTrail];
GO

PRINT 'Starting configuration tables creation...';

BEGIN TRANSACTION;
BEGIN TRY

    -- =============================================
    -- Table: RetentionPolicies
    -- =============================================
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RetentionPolicies' AND schema_id = SCHEMA_ID('config'))
    BEGIN
        PRINT 'Creating table [config].[RetentionPolicies]...';
        
        CREATE TABLE [config].[RetentionPolicies] (
            PolicyId INT IDENTITY(1,1) NOT NULL,
            PolicyName NVARCHAR(100) NOT NULL,
            EntityType NVARCHAR(50) NOT NULL,
            RetentionDays INT NOT NULL,
            ArchiveAfterDays INT NULL,
            DeleteAfterDays INT NULL,
            LegalHold BIT NOT NULL DEFAULT 0,
            LegalHoldReason NVARCHAR(500) NULL,
            LegalHoldDate DATETIME2(7) NULL,
            LegalHoldBy UNIQUEIDENTIFIER NULL,
            IsActive BIT NOT NULL DEFAULT 1,
            Priority INT NOT NULL DEFAULT 0,
            CreatedBy UNIQUEIDENTIFIER NOT NULL,
            CreatedDate DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
            ModifiedBy UNIQUEIDENTIFIER NULL,
            ModifiedDate DATETIME2(7) NULL,
            
            CONSTRAINT PK_RetentionPolicies PRIMARY KEY CLUSTERED (PolicyId),
            CONSTRAINT UQ_RetentionPolicies_Name UNIQUE (PolicyName),
            CONSTRAINT CHK_RetentionPolicies_Days CHECK (RetentionDays > 0)
        );
        PRINT 'Table [config].[RetentionPolicies] created successfully.';
    END

    -- =============================================
    -- Table: SystemConfiguration
    -- =============================================
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SystemConfiguration' AND schema_id = SCHEMA_ID('config'))
    BEGIN
        PRINT 'Creating table [config].[SystemConfiguration]...';
        
        CREATE TABLE [config].[SystemConfiguration] (
            ConfigId INT IDENTITY(1,1) NOT NULL,
            ConfigCategory NVARCHAR(50) NOT NULL,
            ConfigKey NVARCHAR(100) NOT NULL,
            ConfigValue NVARCHAR(MAX) NOT NULL,
            ConfigType NVARCHAR(50) NOT NULL,
            Description NVARCHAR(500) NULL,
            IsEncrypted BIT NOT NULL DEFAULT 0,
            IsRequired BIT NOT NULL DEFAULT 1,
            DefaultValue NVARCHAR(MAX) NULL,
            ValidationRule NVARCHAR(500) NULL,
            LastModifiedBy UNIQUEIDENTIFIER NOT NULL,
            LastModifiedDate DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
            
            CONSTRAINT PK_SystemConfiguration PRIMARY KEY CLUSTERED (ConfigId),
            CONSTRAINT UQ_SystemConfiguration_Key UNIQUE (ConfigCategory, ConfigKey)
        );
        PRINT 'Table [config].[SystemConfiguration] created successfully.';
    END

    -- =============================================
    -- Table: ScheduledJobs
    -- =============================================
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ScheduledJobs' AND schema_id = SCHEMA_ID('config'))
    BEGIN
        PRINT 'Creating table [config].[ScheduledJobs]...';
        
        CREATE TABLE [config].[ScheduledJobs] (
            JobId INT IDENTITY(1,1) NOT NULL,
            JobName NVARCHAR(100) NOT NULL,
            JobType NVARCHAR(50) NOT NULL,
            JobDescription NVARCHAR(500) NULL,
            CronExpression NVARCHAR(100) NULL,
            FrequencyMinutes INT NULL,
            IsEnabled BIT NOT NULL DEFAULT 1,
            LastRunDate DATETIME2(7) NULL,
            LastRunStatus NVARCHAR(50) NULL,
            LastRunDuration INT NULL,
            LastRunMessage NVARCHAR(MAX) NULL,
            NextRunDate DATETIME2(7) NULL,
            Configuration NVARCHAR(MAX) NULL,
            CreatedDate DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
            CreatedBy UNIQUEIDENTIFIER NOT NULL,
            
            CONSTRAINT PK_ScheduledJobs PRIMARY KEY CLUSTERED (JobId),
            CONSTRAINT UQ_ScheduledJobs_Name UNIQUE (JobName)
        );
        PRINT 'Table [config].[ScheduledJobs] created successfully.';
    END

    COMMIT TRANSACTION;
    PRINT 'Configuration tables transaction committed successfully.';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'ERROR in configuration tables creation:';
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS NVARCHAR(10));
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    RETURN;
END CATCH

-- =============================================
-- Add Foreign Key Constraints
-- =============================================
BEGIN TRY
    PRINT 'Adding foreign key constraints...';

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_RetentionPolicies_CreatedBy')
    BEGIN
        ALTER TABLE [config].[RetentionPolicies] ADD CONSTRAINT FK_RetentionPolicies_CreatedBy 
            FOREIGN KEY (CreatedBy) REFERENCES [auth].[Users](UserId);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SystemConfiguration_ModifiedBy')
    BEGIN
        ALTER TABLE [config].[SystemConfiguration] ADD CONSTRAINT FK_SystemConfiguration_ModifiedBy 
            FOREIGN KEY (LastModifiedBy) REFERENCES [auth].[Users](UserId);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ScheduledJobs_CreatedBy')
    BEGIN
        ALTER TABLE [config].[ScheduledJobs] ADD CONSTRAINT FK_ScheduledJobs_CreatedBy 
            FOREIGN KEY (CreatedBy) REFERENCES [auth].[Users](UserId);
    END

    PRINT 'Foreign key constraints added successfully.';

END TRY
BEGIN CATCH
    PRINT 'ERROR adding foreign key constraints:';
    PRINT 'Error Message: ' + ERROR_MESSAGE();
END CATCH

-- =============================================
-- Create Indexes
-- =============================================
BEGIN TRY
    PRINT 'Creating indexes...';

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RetentionPolicies_EntityType')
    BEGIN
        CREATE NONCLUSTERED INDEX IX_RetentionPolicies_EntityType 
        ON [config].[RetentionPolicies](EntityType, IsActive) 
        INCLUDE (RetentionDays, ArchiveAfterDays, LegalHold);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SystemConfiguration_Category')
    BEGIN
        CREATE NONCLUSTERED INDEX IX_SystemConfiguration_Category 
        ON [config].[SystemConfiguration](ConfigCategory) 
        INCLUDE (ConfigKey, ConfigValue);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ScheduledJobs_NextRun')
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ScheduledJobs_NextRun 
        ON [config].[ScheduledJobs](NextRunDate, IsEnabled) 
        INCLUDE (JobName, JobType);
    END

    PRINT 'Configuration table indexes created successfully.';

END TRY
BEGIN CATCH
    PRINT 'ERROR creating indexes:';
    PRINT 'Error Message: ' + ERROR_MESSAGE();
END CATCH

PRINT 'Configuration and retention policy tables setup completed successfully.';
GO
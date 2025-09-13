-- Application Logging Schema
-- Creates table for Serilog application logs
-- Separate from audit trail for regulatory compliance

USE [AuditTrail]
GO

-- Create logging schema for application logs
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'logging')
    EXEC('CREATE SCHEMA [logging]')
GO

-- Application Logs Table for Serilog
-- This is separate from the audit trail tables to maintain compliance
CREATE TABLE [logging].[ApplicationLogs] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [Message] NVARCHAR(MAX) NULL,
    [MessageTemplate] NVARCHAR(MAX) NULL,
    [Level] NVARCHAR(128) NULL,
    [TimeStamp] DATETIME2(7) NOT NULL,
    [Exception] NVARCHAR(MAX) NULL,
    [Properties] NVARCHAR(MAX) NULL,
    [LogEvent] NVARCHAR(MAX) NULL,
    [Application] NVARCHAR(100) NULL,     -- 'API' or 'Web'
    [RequestId] NVARCHAR(100) NULL,       -- Correlation ID
    [UserId] UNIQUEIDENTIFIER NULL,       -- User context
    [UserName] NVARCHAR(256) NULL,        -- Username for quick lookup
    [IpAddress] NVARCHAR(45) NULL,        -- Client IP
    [UserAgent] NVARCHAR(500) NULL,       -- Browser info
    [RequestPath] NVARCHAR(500) NULL,     -- API endpoint or web page
    [StatusCode] INT NULL,                -- HTTP status code
    [ResponseTime] BIGINT NULL,           -- Milliseconds
    [MachineName] NVARCHAR(100) NULL,     -- Server identifier
    [ProcessId] INT NULL,                 -- Process ID
    [ThreadId] INT NULL,                  -- Thread ID
    
    CONSTRAINT [PK_ApplicationLogs] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ApplicationLogs_Users] FOREIGN KEY ([UserId]) 
        REFERENCES [auth].[Users]([UserId]) ON DELETE SET NULL
)
GO

-- Create indexes for performance
CREATE NONCLUSTERED INDEX [IX_ApplicationLogs_TimeStamp] ON [logging].[ApplicationLogs]
(
    [TimeStamp] DESC
)
GO

CREATE NONCLUSTERED INDEX [IX_ApplicationLogs_Level_TimeStamp] ON [logging].[ApplicationLogs]
(
    [Level] ASC,
    [TimeStamp] DESC
)
GO

CREATE NONCLUSTERED INDEX [IX_ApplicationLogs_Application_TimeStamp] ON [logging].[ApplicationLogs]
(
    [Application] ASC,
    [TimeStamp] DESC
)
GO

CREATE NONCLUSTERED INDEX [IX_ApplicationLogs_UserId_TimeStamp] ON [logging].[ApplicationLogs]
(
    [UserId] ASC,
    [TimeStamp] DESC
) WHERE [UserId] IS NOT NULL
GO

CREATE NONCLUSTERED INDEX [IX_ApplicationLogs_RequestId] ON [logging].[ApplicationLogs]
(
    [RequestId] ASC
) WHERE [RequestId] IS NOT NULL
GO

-- Create view for easy querying
CREATE VIEW [logging].[vw_ApplicationLogs] AS
SELECT 
    al.[Id],
    al.[Message],
    al.[Level],
    al.[TimeStamp],
    al.[Exception],
    al.[Application],
    al.[RequestId],
    al.[UserId],
    al.[UserName],
    COALESCE(u.[Username], al.[UserName]) AS [EffectiveUserName],
    u.[FirstName] + ' ' + u.[LastName] AS [FullName],
    al.[IpAddress],
    al.[RequestPath],
    al.[StatusCode],
    al.[ResponseTime],
    al.[MachineName],
    CASE 
        WHEN al.[Level] = 'Error' OR al.[Level] = 'Fatal' THEN 1
        WHEN al.[Level] = 'Warning' THEN 2
        WHEN al.[Level] = 'Information' THEN 3
        WHEN al.[Level] = 'Debug' THEN 4
        WHEN al.[Level] = 'Verbose' THEN 5
        ELSE 6
    END AS [LevelOrder]
FROM [logging].[ApplicationLogs] al
LEFT JOIN [auth].[Users] u ON al.[UserId] = u.[UserId]
GO

-- Create stored procedure for log cleanup
CREATE PROCEDURE [logging].[sp_CleanupApplicationLogs]
    @RetentionDays INT = 90,
    @BatchSize INT = 1000
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -@RetentionDays, GETUTCDATE());
    DECLARE @RowsDeleted INT = @BatchSize;
    DECLARE @TotalDeleted INT = 0;
    
    WHILE @RowsDeleted = @BatchSize
    BEGIN
        DELETE TOP (@BatchSize) 
        FROM [logging].[ApplicationLogs]
        WHERE [TimeStamp] < @CutoffDate;
        
        SET @RowsDeleted = @@ROWCOUNT;
        SET @TotalDeleted = @TotalDeleted + @RowsDeleted;
        
        -- Brief pause to avoid blocking
        WAITFOR DELAY '00:00:01';
    END
    
    PRINT 'Deleted ' + CAST(@TotalDeleted AS NVARCHAR(10)) + ' log records older than ' + 
          CAST(@RetentionDays AS NVARCHAR(10)) + ' days.';
END
GO

-- Grant permissions
GRANT SELECT, INSERT ON [logging].[ApplicationLogs] TO [db_datareader], [db_datawriter]
GO
GRANT SELECT ON [logging].[vw_ApplicationLogs] TO [db_datareader]
GO
GRANT EXECUTE ON [logging].[sp_CleanupApplicationLogs] TO [db_datawriter]
GO

PRINT 'Application logging schema created successfully.'
PRINT 'Table: [logging].[ApplicationLogs]'
PRINT 'View: [logging].[vw_ApplicationLogs]'
PRINT 'Procedure: [logging].[sp_CleanupApplicationLogs]'
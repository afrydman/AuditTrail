-- Separate Logging Tables by Project
-- Creates dedicated tables for API and Web application logs
-- Maintains separation between different application components

USE [AuditTrail]
GO

-- Drop the generic ApplicationLogs table if it exists
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[logging].[ApplicationLogs]') AND type in (N'U'))
    DROP TABLE [logging].[ApplicationLogs]
GO

-- API Application Logs Table
CREATE TABLE [logging].[APILogs] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [Message] NVARCHAR(MAX) NULL,
    [MessageTemplate] NVARCHAR(MAX) NULL,
    [Level] NVARCHAR(128) NULL,
    [TimeStamp] DATETIME2(7) NOT NULL,
    [Exception] NVARCHAR(MAX) NULL,
    [Properties] NVARCHAR(MAX) NULL,
    [LogEvent] NVARCHAR(MAX) NULL,
    [Application] NVARCHAR(100) NULL DEFAULT 'API',
    [RequestId] NVARCHAR(100) NULL,       -- Correlation ID
    [UserId] UNIQUEIDENTIFIER NULL,       -- User context
    [UserName] NVARCHAR(256) NULL,        -- Username for quick lookup
    [IpAddress] NVARCHAR(45) NULL,        -- Client IP
    [UserAgent] NVARCHAR(500) NULL,       -- Browser info
    [RequestPath] NVARCHAR(500) NULL,     -- API endpoint
    [RequestMethod] NVARCHAR(20) NULL,    -- HTTP method
    [StatusCode] INT NULL,                -- HTTP status code
    [ResponseTime] BIGINT NULL,           -- Milliseconds
    [MachineName] NVARCHAR(100) NULL,     -- Server identifier
    [ProcessId] INT NULL,                 -- Process ID
    [ThreadId] INT NULL,                  -- Thread ID
    
    CONSTRAINT [PK_APILogs] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_APILogs_Users] FOREIGN KEY ([UserId]) 
        REFERENCES [auth].[Users]([UserId]) ON DELETE SET NULL
)
GO

-- Web Application Logs Table
CREATE TABLE [logging].[WebLogs] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [Message] NVARCHAR(MAX) NULL,
    [MessageTemplate] NVARCHAR(MAX) NULL,
    [Level] NVARCHAR(128) NULL,
    [TimeStamp] DATETIME2(7) NOT NULL,
    [Exception] NVARCHAR(MAX) NULL,
    [Properties] NVARCHAR(MAX) NULL,
    [LogEvent] NVARCHAR(MAX) NULL,
    [Application] NVARCHAR(100) NULL DEFAULT 'Web',
    [RequestId] NVARCHAR(100) NULL,       -- Correlation ID
    [UserId] UNIQUEIDENTIFIER NULL,       -- User context
    [UserName] NVARCHAR(256) NULL,        -- Username for quick lookup
    [IpAddress] NVARCHAR(45) NULL,        -- Client IP
    [UserAgent] NVARCHAR(500) NULL,       -- Browser info
    [RequestPath] NVARCHAR(500) NULL,     -- Web page path
    [RequestMethod] NVARCHAR(20) NULL,    -- HTTP method
    [StatusCode] INT NULL,                -- HTTP status code
    [ResponseTime] BIGINT NULL,           -- Milliseconds
    [Referrer] NVARCHAR(500) NULL,        -- Referrer URL
    [SessionId] NVARCHAR(100) NULL,       -- Web session ID
    [MachineName] NVARCHAR(100) NULL,     -- Server identifier
    [ProcessId] INT NULL,                 -- Process ID
    [ThreadId] INT NULL,                  -- Thread ID
    
    CONSTRAINT [PK_WebLogs] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_WebLogs_Users] FOREIGN KEY ([UserId]) 
        REFERENCES [auth].[Users]([UserId]) ON DELETE SET NULL
)
GO

-- Create indexes for API logs
CREATE NONCLUSTERED INDEX [IX_APILogs_TimeStamp] ON [logging].[APILogs]
(
    [TimeStamp] DESC
)
GO

CREATE NONCLUSTERED INDEX [IX_APILogs_Level_TimeStamp] ON [logging].[APILogs]
(
    [Level] ASC,
    [TimeStamp] DESC
)
GO

CREATE NONCLUSTERED INDEX [IX_APILogs_UserId_TimeStamp] ON [logging].[APILogs]
(
    [UserId] ASC,
    [TimeStamp] DESC
) WHERE [UserId] IS NOT NULL
GO

CREATE NONCLUSTERED INDEX [IX_APILogs_RequestId] ON [logging].[APILogs]
(
    [RequestId] ASC
) WHERE [RequestId] IS NOT NULL
GO

CREATE NONCLUSTERED INDEX [IX_APILogs_RequestPath] ON [logging].[APILogs]
(
    [RequestPath] ASC,
    [TimeStamp] DESC
) WHERE [RequestPath] IS NOT NULL
GO

-- Create indexes for Web logs
CREATE NONCLUSTERED INDEX [IX_WebLogs_TimeStamp] ON [logging].[WebLogs]
(
    [TimeStamp] DESC
)
GO

CREATE NONCLUSTERED INDEX [IX_WebLogs_Level_TimeStamp] ON [logging].[WebLogs]
(
    [Level] ASC,
    [TimeStamp] DESC
)
GO

CREATE NONCLUSTERED INDEX [IX_WebLogs_UserId_TimeStamp] ON [logging].[WebLogs]
(
    [UserId] ASC,
    [TimeStamp] DESC
) WHERE [UserId] IS NOT NULL
GO

CREATE NONCLUSTERED INDEX [IX_WebLogs_RequestId] ON [logging].[WebLogs]
(
    [RequestId] ASC
) WHERE [RequestId] IS NOT NULL
GO

CREATE NONCLUSTERED INDEX [IX_WebLogs_RequestPath] ON [logging].[WebLogs]
(
    [RequestPath] ASC,
    [TimeStamp] DESC
) WHERE [RequestPath] IS NOT NULL
GO

CREATE NONCLUSTERED INDEX [IX_WebLogs_SessionId] ON [logging].[WebLogs]
(
    [SessionId] ASC,
    [TimeStamp] DESC
) WHERE [SessionId] IS NOT NULL
GO

-- Create unified view for querying all application logs
CREATE VIEW [logging].[vw_AllApplicationLogs] AS
SELECT 
    'API' AS [Source],
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
    al.[RequestMethod],
    al.[StatusCode],
    al.[ResponseTime],
    al.[MachineName],
    NULL AS [Referrer],
    NULL AS [SessionId],
    CASE 
        WHEN al.[Level] = 'Error' OR al.[Level] = 'Fatal' THEN 1
        WHEN al.[Level] = 'Warning' THEN 2
        WHEN al.[Level] = 'Information' THEN 3
        WHEN al.[Level] = 'Debug' THEN 4
        WHEN al.[Level] = 'Verbose' THEN 5
        ELSE 6
    END AS [LevelOrder]
FROM [logging].[APILogs] al
LEFT JOIN [auth].[Users] u ON al.[UserId] = u.[UserId]

UNION ALL

SELECT 
    'Web' AS [Source],
    wl.[Id],
    wl.[Message],
    wl.[Level],
    wl.[TimeStamp],
    wl.[Exception],
    wl.[Application],
    wl.[RequestId],
    wl.[UserId],
    wl.[UserName],
    COALESCE(u.[Username], wl.[UserName]) AS [EffectiveUserName],
    u.[FirstName] + ' ' + u.[LastName] AS [FullName],
    wl.[IpAddress],
    wl.[RequestPath],
    wl.[RequestMethod],
    wl.[StatusCode],
    wl.[ResponseTime],
    wl.[MachineName],
    wl.[Referrer],
    wl.[SessionId],
    CASE 
        WHEN wl.[Level] = 'Error' OR wl.[Level] = 'Fatal' THEN 1
        WHEN wl.[Level] = 'Warning' THEN 2
        WHEN wl.[Level] = 'Information' THEN 3
        WHEN wl.[Level] = 'Debug' THEN 4
        WHEN wl.[Level] = 'Verbose' THEN 5
        ELSE 6
    END AS [LevelOrder]
FROM [logging].[WebLogs] wl
LEFT JOIN [auth].[Users] u ON wl.[UserId] = u.[UserId]
GO

-- Create API-specific view
CREATE VIEW [logging].[vw_APILogs] AS
SELECT 
    al.[Id],
    al.[Message],
    al.[Level],
    al.[TimeStamp],
    al.[Exception],
    al.[RequestId],
    al.[UserId],
    al.[UserName],
    COALESCE(u.[Username], al.[UserName]) AS [EffectiveUserName],
    u.[FirstName] + ' ' + u.[LastName] AS [FullName],
    al.[IpAddress],
    al.[RequestPath],
    al.[RequestMethod],
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
FROM [logging].[APILogs] al
LEFT JOIN [auth].[Users] u ON al.[UserId] = u.[UserId]
GO

-- Create Web-specific view
CREATE VIEW [logging].[vw_WebLogs] AS
SELECT 
    wl.[Id],
    wl.[Message],
    wl.[Level],
    wl.[TimeStamp],
    wl.[Exception],
    wl.[RequestId],
    wl.[UserId],
    wl.[UserName],
    COALESCE(u.[Username], wl.[UserName]) AS [EffectiveUserName],
    u.[FirstName] + ' ' + u.[LastName] AS [FullName],
    wl.[IpAddress],
    wl.[RequestPath],
    wl.[RequestMethod],
    wl.[StatusCode],
    wl.[ResponseTime],
    wl.[Referrer],
    wl.[SessionId],
    wl.[MachineName],
    CASE 
        WHEN wl.[Level] = 'Error' OR wl.[Level] = 'Fatal' THEN 1
        WHEN wl.[Level] = 'Warning' THEN 2
        WHEN wl.[Level] = 'Information' THEN 3
        WHEN wl.[Level] = 'Debug' THEN 4
        WHEN wl.[Level] = 'Verbose' THEN 5
        ELSE 6
    END AS [LevelOrder]
FROM [logging].[WebLogs] wl
LEFT JOIN [auth].[Users] u ON wl.[UserId] = u.[UserId]
GO

-- Create cleanup procedures for each table
CREATE PROCEDURE [logging].[sp_CleanupAPILogs]
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
        FROM [logging].[APILogs]
        WHERE [TimeStamp] < @CutoffDate;
        
        SET @RowsDeleted = @@ROWCOUNT;
        SET @TotalDeleted = @TotalDeleted + @RowsDeleted;
        
        -- Brief pause to avoid blocking
        WAITFOR DELAY '00:00:01';
    END
    
    PRINT 'Deleted ' + CAST(@TotalDeleted AS NVARCHAR(10)) + ' API log records older than ' + 
          CAST(@RetentionDays AS NVARCHAR(10)) + ' days.';
END
GO

CREATE PROCEDURE [logging].[sp_CleanupWebLogs]
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
        FROM [logging].[WebLogs]
        WHERE [TimeStamp] < @CutoffDate;
        
        SET @RowsDeleted = @@ROWCOUNT;
        SET @TotalDeleted = @TotalDeleted + @RowsDeleted;
        
        -- Brief pause to avoid blocking
        WAITFOR DELAY '00:00:01';
    END
    
    PRINT 'Deleted ' + CAST(@TotalDeleted AS NVARCHAR(10)) + ' Web log records older than ' + 
          CAST(@RetentionDays AS NVARCHAR(10)) + ' days.';
END
GO

-- Create master cleanup procedure
CREATE PROCEDURE [logging].[sp_CleanupAllLogs]
    @RetentionDays INT = 90,
    @BatchSize INT = 1000
AS
BEGIN
    SET NOCOUNT ON;
    
    PRINT 'Starting cleanup of all application logs...';
    
    EXEC [logging].[sp_CleanupAPILogs] @RetentionDays, @BatchSize;
    EXEC [logging].[sp_CleanupWebLogs] @RetentionDays, @BatchSize;
    
    PRINT 'Cleanup completed.';
END
GO

-- Grant permissions
GRANT SELECT, INSERT ON [logging].[APILogs] TO [db_datareader], [db_datawriter]
GO
GRANT SELECT, INSERT ON [logging].[WebLogs] TO [db_datareader], [db_datawriter]
GO
GRANT SELECT ON [logging].[vw_AllApplicationLogs] TO [db_datareader]
GO
GRANT SELECT ON [logging].[vw_APILogs] TO [db_datareader]
GO
GRANT SELECT ON [logging].[vw_WebLogs] TO [db_datareader]
GO
GRANT EXECUTE ON [logging].[sp_CleanupAPILogs] TO [db_datawriter]
GO
GRANT EXECUTE ON [logging].[sp_CleanupWebLogs] TO [db_datawriter]
GO
GRANT EXECUTE ON [logging].[sp_CleanupAllLogs] TO [db_datawriter]
GO

PRINT 'Separate project logging schema created successfully.'
PRINT 'Tables: [logging].[APILogs], [logging].[WebLogs]'
PRINT 'Views: [logging].[vw_AllApplicationLogs], [logging].[vw_APILogs], [logging].[vw_WebLogs]'
PRINT 'Procedures: [logging].[sp_CleanupAPILogs], [logging].[sp_CleanupWebLogs], [logging].[sp_CleanupAllLogs]'
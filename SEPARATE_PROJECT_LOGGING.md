# Separate Project Logging Implementation

## Overview
Implemented dedicated logging tables and configuration for API and Web projects, providing better separation of concerns and easier monitoring of each application component.

## Database Schema

### Tables Created
- **`logging.APILogs`** - Dedicated table for API application logs
- **`logging.WebLogs`** - Dedicated table for Web application logs  
- **Replaced**: Generic `logging.ApplicationLogs` table

### Table Structure

#### APILogs Table
```sql
CREATE TABLE [logging].[APILogs] (
    [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
    [Message] NVARCHAR(MAX) NULL,
    [MessageTemplate] NVARCHAR(MAX) NULL,
    [Level] NVARCHAR(128) NULL,
    [TimeStamp] DATETIME2(7) NOT NULL,
    [Exception] NVARCHAR(MAX) NULL,
    [Properties] NVARCHAR(MAX) NULL,
    [LogEvent] NVARCHAR(MAX) NULL,
    [Application] NVARCHAR(100) DEFAULT 'API',
    [RequestId] NVARCHAR(100) NULL,
    [UserId] UNIQUEIDENTIFIER NULL,
    [UserName] NVARCHAR(256) NULL,
    [IpAddress] NVARCHAR(45) NULL,
    [UserAgent] NVARCHAR(500) NULL,
    [RequestPath] NVARCHAR(500) NULL,
    [RequestMethod] NVARCHAR(20) NULL,
    [StatusCode] INT NULL,
    [ResponseTime] BIGINT NULL,
    [MachineName] NVARCHAR(100) NULL,
    [ProcessId] INT NULL,
    [ThreadId] INT NULL
);
```

#### WebLogs Table
```sql
CREATE TABLE [logging].[WebLogs] (
    [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
    [Message] NVARCHAR(MAX) NULL,
    [MessageTemplate] NVARCHAR(MAX) NULL,
    [Level] NVARCHAR(128) NULL,
    [TimeStamp] DATETIME2(7) NOT NULL,
    [Exception] NVARCHAR(MAX) NULL,
    [Properties] NVARCHAR(MAX) NULL,
    [LogEvent] NVARCHAR(MAX) NULL,
    [Application] NVARCHAR(100) DEFAULT 'Web',
    [RequestId] NVARCHAR(100) NULL,
    [UserId] UNIQUEIDENTIFIER NULL,
    [UserName] NVARCHAR(256) NULL,
    [IpAddress] NVARCHAR(45) NULL,
    [UserAgent] NVARCHAR(500) NULL,
    [RequestPath] NVARCHAR(500) NULL,
    [RequestMethod] NVARCHAR(20) NULL,
    [StatusCode] INT NULL,
    [ResponseTime] BIGINT NULL,
    [Referrer] NVARCHAR(500) NULL,        -- Web-specific
    [SessionId] NVARCHAR(100) NULL,       -- Web-specific
    [MachineName] NVARCHAR(100) NULL,
    [ProcessId] INT NULL,
    [ThreadId] INT NULL
);
```

### Differences Between Tables
- **WebLogs** includes additional fields:
  - `Referrer` - HTTP referrer header
  - `SessionId` - Web session identifier
- **APILogs** focuses on API-specific metrics
- Both maintain core logging fields for consistency

## Views Created

### Unified View
- **`logging.vw_AllApplicationLogs`** - Combined view of all logs with source identification

### Project-Specific Views  
- **`logging.vw_APILogs`** - API logs with user joins
- **`logging.vw_WebLogs`** - Web logs with user joins

## Stored Procedures

### Cleanup Procedures
- **`logging.sp_CleanupAPILogs`** - Clean old API logs
- **`logging.sp_CleanupWebLogs`** - Clean old Web logs  
- **`logging.sp_CleanupAllLogs`** - Master cleanup procedure

### Usage
```sql
-- Clean logs older than 90 days (default)
EXEC logging.sp_CleanupAllLogs;

-- Clean with custom retention
EXEC logging.sp_CleanupAPILogs @RetentionDays = 60, @BatchSize = 500;
```

## Serilog Configuration Changes

### API Project
```csharp
.WriteTo.MSSqlServer(
    connectionString: "Server=.;Database=AuditTrail;Trusted_Connection=true;TrustServerCertificate=true;",
    sinkOptions: new MSSqlServerSinkOptions
    {
        TableName = "APILogs",        // Changed from ApplicationLogs
        SchemaName = "logging",
        AutoCreateSqlTable = false
    },
    restrictedToMinimumLevel: LogEventLevel.Information)
```

### Web Project  
```csharp
.WriteTo.MSSqlServer(
    connectionString: "Server=.;Database=AuditTrail;Trusted_Connection=true;TrustServerCertificate=true;",
    sinkOptions: new MSSqlServerSinkOptions
    {
        TableName = "WebLogs",           // Changed from ApplicationLogs
        SchemaName = "logging",
        AutoCreateSqlTable = false
    },
    restrictedToMinimumLevel: LogEventLevel.Information)
```

## Enhanced Middleware Features

### API Request Logging
- Enhanced with structured logging scopes
- Captures API-specific context:
  - Request method and path
  - Response time and status codes
  - User context and IP address
  - Content type and size

### Web Request Logging  
- Added session support and tracking
- Captures Web-specific context:
  - Session ID tracking
  - HTTP referrer information
  - Enhanced user context
  - Static file filtering

### Session Support Added
```csharp
// Added to Web project
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "AuditTrailSession";
});
```

## Benefits of Separation

### 1. **Improved Performance**
- Smaller table sizes for each project
- More targeted indexing strategies
- Reduced query complexity

### 2. **Better Monitoring**
- Project-specific log analysis
- Tailored retention policies
- Independent scaling

### 3. **Enhanced Context**
- Web logs include session and referrer data
- API logs focus on endpoint performance
- Specialized fields per application type

### 4. **Maintenance Benefits**
- Separate cleanup schedules
- Project-specific log rotation
- Independent troubleshooting

## File Logging Structure

### File Separation Maintained
- **API**: `logs/api-yyyy-MM-dd.log`  
- **Web**: `logs/web-yyyy-MM-dd.log`
- Daily rotation with full debug information

### Log Format
```
[2023-09-13 14:30:25.123 INF] [API] Request completed: GET /api/users | Status: 200 | Duration: 45ms {"RequestId":"123","UserId":"user1"}
[2023-09-13 14:30:30.456 INF] [Web] Web request completed: GET /dashboard | Status: 200 | Duration: 120ms | Session: sess123 {"SessionId":"sess123","Referrer":"https://example.com"}
```

## Querying Examples

### Get All API Errors from Last Day
```sql
SELECT * FROM logging.vw_APILogs 
WHERE Level = 'Error' 
AND TimeStamp >= DATEADD(day, -1, GETUTCDATE())
ORDER BY TimeStamp DESC;
```

### Get Web Session Activity
```sql  
SELECT * FROM logging.vw_WebLogs
WHERE SessionId = 'specific-session-id'
ORDER BY TimeStamp ASC;
```

### Combined Error Analysis
```sql
SELECT Source, COUNT(*) as ErrorCount, AVG(ResponseTime) as AvgResponseTime
FROM logging.vw_AllApplicationLogs  
WHERE Level = 'Error'
AND TimeStamp >= DATEADD(hour, -1, GETUTCDATE())
GROUP BY Source;
```

## Migration Notes

### From Previous Implementation
- Old `ApplicationLogs` table was dropped
- No data migration required (fresh implementation)
- All configurations updated to use new tables

### Future Considerations
- Consider partitioning for high-volume scenarios
- Implement log archival for long-term retention
- Add alerting based on error patterns per project

## Status
✅ **Implementation Complete**
- Database tables created and indexed
- Serilog configurations updated  
- Middleware enhanced with structured logging
- Views and cleanup procedures implemented
- Session support added to Web project
- All projects building successfully
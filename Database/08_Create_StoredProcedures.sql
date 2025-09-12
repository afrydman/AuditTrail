-- =============================================
-- Stored Procedures for Audit Trail System
-- Re-runnable: YES
-- =============================================

SET NOCOUNT ON;
USE [AuditTrail];
GO

PRINT 'Starting stored procedures creation...';

-- =============================================
-- SP: Log Audit Event
-- =============================================
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_LogAuditEvent' AND schema_id = SCHEMA_ID('audit'))
    DROP PROCEDURE [audit].[sp_LogAuditEvent];
GO

CREATE PROCEDURE [audit].[sp_LogAuditEvent]
    @EventType NVARCHAR(100),
    @EventCategory NVARCHAR(50),
    @UserId UNIQUEIDENTIFIER = NULL,
    @Username NVARCHAR(100) = NULL,
    @RoleName NVARCHAR(50) = NULL,
    @IPAddress NVARCHAR(45) = NULL,
    @UserAgent NVARCHAR(500) = NULL,
    @SessionId UNIQUEIDENTIFIER = NULL,
    @EntityType NVARCHAR(100) = NULL,
    @EntityId NVARCHAR(100) = NULL,
    @EntityName NVARCHAR(255) = NULL,
    @Action NVARCHAR(100),
    @OldValue NVARCHAR(MAX) = NULL,
    @NewValue NVARCHAR(MAX) = NULL,
    @AdditionalData NVARCHAR(MAX) = NULL,
    @Result NVARCHAR(50) = 'Success',
    @ErrorMessage NVARCHAR(MAX) = NULL,
    @Duration INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        DECLARE @AuditId UNIQUEIDENTIFIER = NEWID();
        DECLARE @AppVersion NVARCHAR(50);
        
        SELECT @AppVersion = ConfigValue 
        FROM [config].[SystemConfiguration] 
        WHERE ConfigKey = 'ApplicationVersion';
        
        INSERT INTO [audit].[AuditTrail] (
            AuditId, EventType, EventCategory, Timestamp, UserId, Username, RoleName,
            IPAddress, UserAgent, SessionId, EntityType, EntityId, EntityName,
            Action, OldValue, NewValue, AdditionalData, Result, ErrorMessage,
            Duration, ServerName, ApplicationVersion
        )
        VALUES (
            @AuditId, @EventType, @EventCategory, SYSUTCDATETIME(), @UserId, @Username, @RoleName,
            @IPAddress, @UserAgent, @SessionId, @EntityType, @EntityId, @EntityName,
            @Action, @OldValue, @NewValue, @AdditionalData, @Result, @ErrorMessage,
            @Duration, @@SERVERNAME, ISNULL(@AppVersion, '1.0.0')
        );
        
        SELECT @AuditId AS AuditId;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- =============================================
-- SP: Log File Operation
-- =============================================
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_LogFileOperation' AND schema_id = SCHEMA_ID('audit'))
    DROP PROCEDURE [audit].[sp_LogFileOperation];
GO

CREATE PROCEDURE [audit].[sp_LogFileOperation]
    @FileId UNIQUEIDENTIFIER,
    @FileName NVARCHAR(255),
    @FilePath NVARCHAR(500),
    @FileVersion INT = NULL,
    @Operation NVARCHAR(50),
    @UserId UNIQUEIDENTIFIER,
    @Username NVARCHAR(100),
    @IPAddress NVARCHAR(45),
    @AccessGranted BIT = 1,
    @DenialReason NVARCHAR(500) = NULL,
    @FileSize BIGINT = NULL,
    @Checksum NVARCHAR(64) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        DECLARE @AuditId UNIQUEIDENTIFIER;
        DECLARE @EventType NVARCHAR(100);
        DECLARE @Result NVARCHAR(50);
        
        SET @EventType = 'FILE_' + UPPER(@Operation);
        SET @Result = CASE WHEN @AccessGranted = 1 THEN 'Success' ELSE 'Failure' END;
        
        -- Log to main audit trail
        EXEC [audit].[sp_LogAuditEvent]
            @EventType = @EventType,
            @EventCategory = 'File',
            @UserId = @UserId,
            @Username = @Username,
            @IPAddress = @IPAddress,
            @EntityType = 'File',
            @EntityId = @FileId,
            @EntityName = @FileName,
            @Action = @Operation,
            @Result = @Result,
            @ErrorMessage = @DenialReason;
        
        -- Get the audit ID
        SELECT TOP 1 @AuditId = AuditId 
        FROM [audit].[AuditTrail] 
        WHERE UserId = @UserId AND EntityId = @FileId 
        ORDER BY Timestamp DESC;
        
        -- Log to file audit trail
        INSERT INTO [audit].[FileAuditTrail] (
            AuditId, FileId, FileName, FilePath, FileVersion,
            Operation, FileSize, Checksum, AccessGranted, DenialReason
        )
        VALUES (
            @AuditId, @FileId, @FileName, @FilePath, @FileVersion,
            @Operation, @FileSize, @Checksum, @AccessGranted, @DenialReason
        );
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- =============================================
-- SP: Authenticate User
-- =============================================
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_AuthenticateUser' AND schema_id = SCHEMA_ID('auth'))
    DROP PROCEDURE [auth].[sp_AuthenticateUser];
GO

CREATE PROCEDURE [auth].[sp_AuthenticateUser]
    @Username NVARCHAR(100),
    @PasswordHash NVARCHAR(500),
    @IPAddress NVARCHAR(45),
    @UserAgent NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        DECLARE @UserId UNIQUEIDENTIFIER;
        DECLARE @StoredHash NVARCHAR(500);
        DECLARE @IsActive BIT;
        DECLARE @IsLocked BIT;
        DECLARE @LockoutEnd DATETIME2;
        DECLARE @FailedAttempts INT;
        DECLARE @MaxAttempts INT;
        DECLARE @Success BIT = 0;
        DECLARE @Message NVARCHAR(500);
        
        -- Get max attempts from config
        SELECT @MaxAttempts = CAST(ConfigValue AS INT) 
        FROM [config].[SystemConfiguration] 
        WHERE ConfigKey = 'MaxLoginAttempts';
        
        IF @MaxAttempts IS NULL SET @MaxAttempts = 5;
        
        -- Get user details
        SELECT 
            @UserId = UserId,
            @StoredHash = PasswordHash,
            @IsActive = IsActive,
            @IsLocked = IsLocked,
            @LockoutEnd = LockoutEnd,
            @FailedAttempts = FailedLoginAttempts
        FROM [auth].[Users]
        WHERE Username = @Username;
        
        -- Check if user exists
        IF @UserId IS NULL
        BEGIN
            SET @Message = 'Invalid username or password';
            GOTO LogAttempt;
        END
        
        -- Check if account is active
        IF @IsActive = 0
        BEGIN
            SET @Message = 'Account is deactivated';
            GOTO LogAttempt;
        END
        
        -- Check if account is locked
        IF @IsLocked = 1 AND (@LockoutEnd IS NULL OR @LockoutEnd > SYSUTCDATETIME())
        BEGIN
            SET @Message = 'Account is locked';
            GOTO LogAttempt;
        END
        
        -- Verify password
        IF @StoredHash != @PasswordHash
        BEGIN
            -- Increment failed attempts
            UPDATE [auth].[Users]
            SET FailedLoginAttempts = FailedLoginAttempts + 1,
                IsLocked = CASE WHEN FailedLoginAttempts + 1 >= @MaxAttempts THEN 1 ELSE 0 END,
                LockoutEnd = CASE WHEN FailedLoginAttempts + 1 >= @MaxAttempts THEN NULL ELSE LockoutEnd END
            WHERE UserId = @UserId;
            
            SET @Message = 'Invalid username or password';
            GOTO LogAttempt;
        END
        
        -- Successful authentication
        SET @Success = 1;
        SET @Message = 'Authentication successful';
        
        -- Reset failed attempts and update last login
        UPDATE [auth].[Users]
        SET FailedLoginAttempts = 0,
            IsLocked = 0,
            LockoutEnd = NULL,
            LastLoginDate = SYSUTCDATETIME(),
            LastLoginIP = @IPAddress
        WHERE UserId = @UserId;
        
LogAttempt:
        -- Log the attempt
        INSERT INTO [auth].[LoginAttempts] (Username, IPAddress, UserAgent, IsSuccessful, FailureReason)
        VALUES (@Username, @IPAddress, @UserAgent, @Success, CASE WHEN @Success = 0 THEN @Message ELSE NULL END);
        
        -- Log to audit trail
        DECLARE @EventType NVARCHAR(100) = CASE WHEN @Success = 1 THEN 'USER_LOGIN' ELSE 'USER_LOGIN_FAILED' END;
        DECLARE @AuditResult NVARCHAR(50) = CASE WHEN @Success = 1 THEN 'Success' ELSE 'Failure' END;
        DECLARE @AuditError NVARCHAR(MAX) = CASE WHEN @Success = 0 THEN @Message ELSE NULL END;
        
        EXEC [audit].[sp_LogAuditEvent]
            @EventType = @EventType,
            @EventCategory = 'User',
            @UserId = @UserId,
            @Username = @Username,
            @IPAddress = @IPAddress,
            @UserAgent = @UserAgent,
            @Action = 'Login',
            @Result = @AuditResult,
            @ErrorMessage = @AuditError;
        
        -- Return result
        IF @Success = 1
        BEGIN
            SELECT 
                @Success AS Success,
                @Message AS Message,
                u.UserId,
                u.Username,
                u.Email,
                u.FirstName,
                u.LastName,
                r.RoleName,
                u.MustChangePassword
            FROM [auth].[Users] u
            INNER JOIN [auth].[Roles] r ON u.RoleId = r.RoleId
            WHERE u.UserId = @UserId;
        END
        ELSE
        BEGIN
            SELECT 
                @Success AS Success,
                @Message AS Message,
                NULL AS UserId,
                NULL AS Username,
                NULL AS Email,
                NULL AS FirstName,
                NULL AS LastName,
                NULL AS RoleName,
                NULL AS MustChangePassword;
        END
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- =============================================
-- SP: Get User Permissions
-- =============================================
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_GetUserPermissions' AND schema_id = SCHEMA_ID('auth'))
    DROP PROCEDURE [auth].[sp_GetUserPermissions];
GO

CREATE PROCEDURE [auth].[sp_GetUserPermissions]
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Get combined permissions (role + user specific)
        WITH RolePermissions AS (
            SELECT 
                p.PermissionCode,
                p.PermissionName,
                p.ResourceType,
                rp.ResourceId,
                rp.AccessLevel,
                'Role' AS SourceType
            FROM [auth].[Users] u
            INNER JOIN [auth].[RolePermissions] rp ON u.RoleId = rp.RoleId
            INNER JOIN [auth].[Permissions] p ON rp.PermissionId = p.PermissionId
            WHERE u.UserId = @UserId 
                AND rp.IsActive = 1
                AND (rp.ExpiresDate IS NULL OR rp.ExpiresDate > SYSUTCDATETIME())
        ),
        UserPermissions AS (
            SELECT 
                p.PermissionCode,
                p.PermissionName,
                p.ResourceType,
                up.ResourceId,
                up.AccessLevel,
                'User' AS SourceType
            FROM [auth].[UserPermissions] up
            INNER JOIN [auth].[Permissions] p ON up.PermissionId = p.PermissionId
            WHERE up.UserId = @UserId 
                AND up.IsActive = 1
                AND up.IsGrant = 1
                AND (up.ExpiresDate IS NULL OR up.ExpiresDate > SYSUTCDATETIME())
        )
        SELECT DISTINCT
            PermissionCode,
            PermissionName,
            ResourceType,
            ResourceId,
            MAX(AccessLevel) AS AccessLevel
        FROM (
            SELECT * FROM RolePermissions
            UNION ALL
            SELECT * FROM UserPermissions
        ) AS Combined
        GROUP BY PermissionCode, PermissionName, ResourceType, ResourceId
        ORDER BY ResourceType, PermissionCode;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- =============================================
-- SP: Upload File Version
-- =============================================
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_UploadFileVersion' AND schema_id = SCHEMA_ID('docs'))
    DROP PROCEDURE [docs].[sp_UploadFileVersion];
GO

CREATE PROCEDURE [docs].[sp_UploadFileVersion]
    @FileName NVARCHAR(255),
    @FilePath NVARCHAR(500),
    @FileSize BIGINT,
    @ContentType NVARCHAR(100),
    @S3Key NVARCHAR(1000),
    @Checksum NVARCHAR(64),
    @UploadedBy UNIQUEIDENTIFIER,
    @StudyId NVARCHAR(100) = NULL,
    @DocumentType NVARCHAR(100) = NULL,
    @Description NVARCHAR(MAX) = NULL,
    @FileId UNIQUEIDENTIFIER OUTPUT,
    @Version INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    
    BEGIN TRY
        DECLARE @BucketName NVARCHAR(255);
        DECLARE @BlobUrl NVARCHAR(1000);
        
        -- Get S3 bucket name from config
        SELECT @BucketName = ConfigValue 
        FROM [config].[SystemConfiguration] 
        WHERE ConfigKey = 'S3BucketName';
        
        IF @BucketName IS NULL SET @BucketName = 'audit-trail-documents';
        
        SET @BlobUrl = 'https://s3.amazonaws.com/' + @BucketName + '/' + @S3Key;
        
        -- Check if file exists
        SELECT TOP 1 
            @FileId = FileId,
            @Version = MAX(Version) + 1
        FROM [docs].[Files]
        WHERE FilePath = @FilePath 
            AND FileName = @FileName 
            AND IsDeleted = 0
        GROUP BY FileId;
        
        IF @FileId IS NULL
        BEGIN
            -- New file
            SET @FileId = NEWID();
            SET @Version = 1;
            
            INSERT INTO [docs].[Files] (
                FileId, FileName, FileExtension, FilePath, Version,
                FileSize, ContentType, BlobUrl, S3BucketName, S3Key,
                Checksum, OriginalFileName, StudyId, DocumentType,
                Description, UploadedBy
            )
            VALUES (
                @FileId, 
                @FileName,
                RIGHT(@FileName, CHARINDEX('.', REVERSE(@FileName))),
                @FilePath,
                @Version,
                @FileSize,
                @ContentType,
                @BlobUrl,
                @BucketName,
                @S3Key,
                @Checksum,
                @FileName,
                @StudyId,
                @DocumentType,
                @Description,
                @UploadedBy
            );
        END
        ELSE
        BEGIN
            -- New version of existing file
            UPDATE [docs].[Files]
            SET Version = @Version,
                FileSize = @FileSize,
                S3Key = @S3Key,
                Checksum = @Checksum,
                UploadedDate = SYSUTCDATETIME(),
                UploadedBy = @UploadedBy
            WHERE FileId = @FileId;
        END
        
        -- Add to version history
        INSERT INTO [docs].[FileVersions] (
            FileId, Version, FileName, FileSize,
            BlobUrl, S3Key, Checksum, CreatedBy
        )
        VALUES (
            @FileId, @Version, @FileName, @FileSize,
            @BlobUrl, @S3Key, @Checksum, @UploadedBy
        );
        
        COMMIT TRANSACTION;
        
        -- Return the file details
        SELECT 
            @FileId AS FileId,
            @Version AS Version,
            @FileName AS FileName,
            @FilePath AS FilePath;
            
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- =============================================
-- SP: Search Audit Trail
-- =============================================
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_SearchAuditTrail' AND schema_id = SCHEMA_ID('audit'))
    DROP PROCEDURE [audit].[sp_SearchAuditTrail];
GO

CREATE PROCEDURE [audit].[sp_SearchAuditTrail]
    @StartDate DATETIME2 = NULL,
    @EndDate DATETIME2 = NULL,
    @UserId UNIQUEIDENTIFIER = NULL,
    @EventType NVARCHAR(100) = NULL,
    @EntityType NVARCHAR(100) = NULL,
    @EntityId NVARCHAR(100) = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 50
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Set defaults
        IF @StartDate IS NULL SET @StartDate = DATEADD(DAY, -30, SYSUTCDATETIME());
        IF @EndDate IS NULL SET @EndDate = SYSUTCDATETIME();
        IF @PageNumber < 1 SET @PageNumber = 1;
        IF @PageSize < 1 SET @PageSize = 50;
        IF @PageSize > 1000 SET @PageSize = 1000; -- Max page size
        
        -- Get total count
        DECLARE @TotalCount INT;
        SELECT @TotalCount = COUNT(*)
        FROM [audit].[AuditTrail]
        WHERE Timestamp BETWEEN @StartDate AND @EndDate
            AND (@UserId IS NULL OR UserId = @UserId)
            AND (@EventType IS NULL OR EventType = @EventType)
            AND (@EntityType IS NULL OR EntityType = @EntityType)
            AND (@EntityId IS NULL OR EntityId = @EntityId);
        
        -- Get paginated results
        SELECT 
            AuditId,
            EventType,
            EventCategory,
            Timestamp,
            UserId,
            Username,
            RoleName,
            IPAddress,
            EntityType,
            EntityId,
            EntityName,
            Action,
            Result,
            ErrorMessage,
            @TotalCount AS TotalCount,
            @PageNumber AS PageNumber,
            @PageSize AS PageSize,
            CEILING(CAST(@TotalCount AS FLOAT) / @PageSize) AS TotalPages
        FROM [audit].[AuditTrail]
        WHERE Timestamp BETWEEN @StartDate AND @EndDate
            AND (@UserId IS NULL OR UserId = @UserId)
            AND (@EventType IS NULL OR EventType = @EventType)
            AND (@EntityType IS NULL OR EntityType = @EntityType)
            AND (@EntityId IS NULL OR EntityId = @EntityId)
        ORDER BY Timestamp DESC
        OFFSET (@PageNumber - 1) * @PageSize ROWS
        FETCH NEXT @PageSize ROWS ONLY;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- =============================================
-- SP: Archive Old Audit Logs
-- =============================================
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_ArchiveAuditLogs' AND schema_id = SCHEMA_ID('audit'))
    DROP PROCEDURE [audit].[sp_ArchiveAuditLogs];
GO

CREATE PROCEDURE [audit].[sp_ArchiveAuditLogs]
    @DaysToKeep INT = 180
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        DECLARE @ArchiveDate DATETIME2 = DATEADD(DAY, -@DaysToKeep, SYSUTCDATETIME());
        DECLARE @ArchivedCount INT;
        DECLARE @AdditionalData NVARCHAR(MAX);
        
        BEGIN TRANSACTION;
        
        -- Copy old records to archive table
        INSERT INTO [audit].[AuditTrailArchive]
        SELECT *, SYSUTCDATETIME() AS ArchivedDate
        FROM [audit].[AuditTrail]
        WHERE Timestamp < @ArchiveDate;
        
        SET @ArchivedCount = @@ROWCOUNT;
        
        -- Note: We cannot delete from AuditTrail due to the trigger
        -- In production, you might partition the table and switch partitions
        
        -- Create JSON for additional data
        SET @AdditionalData = '{"RecordsArchived": ' + CAST(@ArchivedCount AS NVARCHAR(20)) + '}';
        
        -- Log the archive operation
        EXEC [audit].[sp_LogAuditEvent]
            @EventType = 'AUDIT_ARCHIVED',
            @EventCategory = 'System',
            @Action = 'Archive',
            @AdditionalData = @AdditionalData;
        
        COMMIT TRANSACTION;
        
        SELECT @ArchivedCount AS RecordsArchived;
        
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- =============================================
-- SP: Generate Audit Summary
-- =============================================
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_GenerateAuditSummary' AND schema_id = SCHEMA_ID('audit'))
    DROP PROCEDURE [audit].[sp_GenerateAuditSummary];
GO

CREATE PROCEDURE [audit].[sp_GenerateAuditSummary]
    @Date DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        IF @Date IS NULL SET @Date = CAST(DATEADD(DAY, -1, SYSUTCDATETIME()) AS DATE);
        
        -- Delete existing summary for this date
        DELETE FROM [audit].[AuditSummary] WHERE SummaryDate = @Date;
        
        -- Generate new summary
        INSERT INTO [audit].[AuditSummary] (
            SummaryDate, EventCategory, EventType,
            TotalCount, SuccessCount, FailureCount, UniqueUsers
        )
        SELECT 
            @Date,
            EventCategory,
            EventType,
            COUNT(*) AS TotalCount,
            SUM(CASE WHEN Result = 'Success' THEN 1 ELSE 0 END) AS SuccessCount,
            SUM(CASE WHEN Result != 'Success' THEN 1 ELSE 0 END) AS FailureCount,
            COUNT(DISTINCT UserId) AS UniqueUsers
        FROM [audit].[AuditTrail]
        WHERE CAST(Timestamp AS DATE) = @Date
        GROUP BY EventCategory, EventType;
        
        SELECT @@ROWCOUNT AS RecordsCreated;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

PRINT 'Stored procedures created successfully';
GO
-- =============================================
-- Fix Authentication Stored Procedures to work with BCrypt
-- =============================================

USE [AuditTrail];
GO

-- =============================================
-- SP: Log Authentication Attempt (Simple logging only)
-- =============================================
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_LogAuthenticationAttempt' AND schema_id = SCHEMA_ID('auth'))
    DROP PROCEDURE [auth].[sp_LogAuthenticationAttempt];
GO

CREATE PROCEDURE [auth].[sp_LogAuthenticationAttempt]
    @Username NVARCHAR(100),
    @UserId UNIQUEIDENTIFIER = NULL,
    @IsSuccess BIT,
    @FailureReason NVARCHAR(500) = NULL,
    @IPAddress NVARCHAR(45),
    @UserAgent NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Log the attempt
        INSERT INTO [auth].[LoginAttempts] (Username, IPAddress, UserAgent, IsSuccessful, FailureReason)
        VALUES (@Username, @IPAddress, @UserAgent, @IsSuccess, @FailureReason);
        
        -- Log to audit trail
        DECLARE @EventType NVARCHAR(100) = CASE WHEN @IsSuccess = 1 THEN 'USER_LOGIN' ELSE 'USER_LOGIN_FAILED' END;
        DECLARE @AuditResult NVARCHAR(50) = CASE WHEN @IsSuccess = 1 THEN 'Success' ELSE 'Failure' END;
        
        EXEC [audit].[sp_LogAuditEvent]
            @EventType = @EventType,
            @EventCategory = 'User',
            @UserId = @UserId,
            @Username = @Username,
            @IPAddress = @IPAddress,
            @UserAgent = @UserAgent,
            @Action = 'Login',
            @Result = @AuditResult,
            @ErrorMessage = @FailureReason;
            
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- =============================================
-- SP: Process Authentication Result (Handle success/failure logic)
-- =============================================
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_ProcessAuthenticationResult' AND schema_id = SCHEMA_ID('auth'))
    DROP PROCEDURE [auth].[sp_ProcessAuthenticationResult];
GO

CREATE PROCEDURE [auth].[sp_ProcessAuthenticationResult]
    @Username NVARCHAR(100),
    @UserId UNIQUEIDENTIFIER,
    @IsSuccess BIT,
    @FailureReason NVARCHAR(500) = NULL,
    @IPAddress NVARCHAR(45),
    @UserAgent NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        DECLARE @MaxAttempts INT;
        DECLARE @Message NVARCHAR(500);
        
        -- Get max attempts from config
        SELECT @MaxAttempts = CAST(ConfigValue AS INT) 
        FROM [config].[SystemConfiguration] 
        WHERE ConfigKey = 'MaxLoginAttempts';
        
        IF @MaxAttempts IS NULL SET @MaxAttempts = 5;
        
        IF @IsSuccess = 1
        BEGIN
            -- Successful authentication - reset failed attempts and update last login
            UPDATE [auth].[Users]
            SET FailedLoginAttempts = 0,
                IsLocked = 0,
                LockoutEnd = NULL,
                LastLoginDate = SYSUTCDATETIME(),
                LastLoginIP = @IPAddress
            WHERE UserId = @UserId;
            
            SET @Message = 'Authentication successful';
            
            -- Log successful attempt
            EXEC [auth].[sp_LogAuthenticationAttempt]
                @Username = @Username,
                @UserId = @UserId,
                @IsSuccess = 1,
                @IPAddress = @IPAddress,
                @UserAgent = @UserAgent;
            
            -- Return success result with user details
            SELECT 
                1 AS Success,
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
            -- Failed authentication - increment failed attempts
            UPDATE [auth].[Users]
            SET FailedLoginAttempts = FailedLoginAttempts + 1,
                IsLocked = CASE WHEN FailedLoginAttempts + 1 >= @MaxAttempts THEN 1 ELSE IsLocked END,
                LockoutEnd = CASE WHEN FailedLoginAttempts + 1 >= @MaxAttempts THEN NULL ELSE LockoutEnd END
            WHERE UserId = @UserId;
            
            SET @Message = ISNULL(@FailureReason, 'Authentication failed');
            
            -- Log failed attempt
            EXEC [auth].[sp_LogAuthenticationAttempt]
                @Username = @Username,
                @UserId = @UserId,
                @IsSuccess = 0,
                @FailureReason = @Message,
                @IPAddress = @IPAddress,
                @UserAgent = @UserAgent;
            
            -- Return failure result
            SELECT 
                0 AS Success,
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

PRINT 'Authentication stored procedures updated successfully';
PRINT 'BCrypt password verification is now handled in application code';
PRINT 'Stored procedures handle user management and audit logging';
GO
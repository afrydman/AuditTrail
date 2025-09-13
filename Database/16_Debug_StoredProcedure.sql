-- Debug the stored procedure to see what parameters it's receiving
USE [AuditTrail];
GO

-- Create a debug table to log what the SP receives
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[debug].[SPDebug]') AND type in (N'U'))
BEGIN
    CREATE SCHEMA debug;
    CREATE TABLE [debug].[SPDebug] (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(100),
        UserId UNIQUEIDENTIFIER,
        IsSuccess BIT,
        IPAddress NVARCHAR(45),
        CallTime DATETIME2 DEFAULT SYSUTCDATETIME()
    );
END
GO

-- Update the stored procedure to log what it receives
ALTER PROCEDURE [auth].[sp_ProcessAuthenticationResult]
    @Username NVARCHAR(100),
    @UserId UNIQUEIDENTIFIER,
    @IsSuccess BIT,
    @FailureReason NVARCHAR(500) = NULL,
    @IPAddress NVARCHAR(45),
    @UserAgent NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    -- DEBUG: Log what we received
    INSERT INTO [debug].[SPDebug] (Username, UserId, IsSuccess, IPAddress)
    VALUES (@Username, @UserId, @IsSuccess, @IPAddress);
    
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
        -- If there's an error, return failure
        SELECT 
            0 AS Success,
            'Stored procedure error: ' + ERROR_MESSAGE() AS Message,
            NULL AS UserId,
            NULL AS Username,
            NULL AS Email,
            NULL AS FirstName,
            NULL AS LastName,
            NULL AS RoleName,
            NULL AS MustChangePassword;
    END CATCH
END
GO

PRINT 'Debug version of stored procedure created';
GO
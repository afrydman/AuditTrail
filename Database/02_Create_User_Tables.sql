-- =============================================
-- User Management Tables
-- Schema: auth
-- Re-runnable: YES
-- =============================================

SET NOCOUNT ON;
USE [AuditTrail];
GO

PRINT 'Starting user tables creation...';

BEGIN TRANSACTION;
BEGIN TRY

    -- =============================================
    -- Table: Roles
    -- =============================================
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Roles' AND schema_id = SCHEMA_ID('auth'))
    BEGIN
        PRINT 'Creating table [auth].[Roles]...';
        
        CREATE TABLE [auth].[Roles] (
            RoleId INT IDENTITY(1,1) NOT NULL,
            RoleName NVARCHAR(50) NOT NULL,
            Description NVARCHAR(500) NULL,
            IsActive BIT NOT NULL DEFAULT 1,
            CreatedDate DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
            CreatedBy UNIQUEIDENTIFIER NULL,
            ModifiedDate DATETIME2(7) NULL,
            ModifiedBy UNIQUEIDENTIFIER NULL,
            
            CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (RoleId),
            CONSTRAINT UQ_Roles_RoleName UNIQUE (RoleName)
        );
        
        PRINT 'Table [auth].[Roles] created successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Table [auth].[Roles] already exists. Skipping creation.';
    END

    -- =============================================
    -- Table: Users
    -- =============================================
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Users' AND schema_id = SCHEMA_ID('auth'))
    BEGIN
        PRINT 'Creating table [auth].[Users]...';
        
        CREATE TABLE [auth].[Users] (
            UserId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
            Username NVARCHAR(100) NOT NULL,
            Email NVARCHAR(255) NOT NULL,
            PasswordHash NVARCHAR(500) NOT NULL,
            PasswordSalt NVARCHAR(500) NOT NULL,
            FirstName NVARCHAR(100) NOT NULL,
            LastName NVARCHAR(100) NOT NULL,
            RoleId INT NOT NULL,
            IsActive BIT NOT NULL DEFAULT 1,
            IsEmailVerified BIT NOT NULL DEFAULT 0,
            EmailVerificationToken NVARCHAR(500) NULL,
            EmailVerificationExpiry DATETIME2(7) NULL,
            PasswordResetToken NVARCHAR(500) NULL,
            PasswordResetExpiry DATETIME2(7) NULL,
            FailedLoginAttempts INT NOT NULL DEFAULT 0,
            IsLocked BIT NOT NULL DEFAULT 0,
            LockoutEnd DATETIME2(7) NULL,
            LastPasswordChangeDate DATETIME2(7) NULL,
            MustChangePassword BIT NOT NULL DEFAULT 0,
            CreatedDate DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
            CreatedBy UNIQUEIDENTIFIER NULL,
            ModifiedDate DATETIME2(7) NULL,
            ModifiedBy UNIQUEIDENTIFIER NULL,
            LastLoginDate DATETIME2(7) NULL,
            LastLoginIP NVARCHAR(45) NULL,
            
            CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (UserId),
            CONSTRAINT UQ_Users_Username UNIQUE (Username),
            CONSTRAINT UQ_Users_Email UNIQUE (Email),
            CONSTRAINT CHK_Users_Email CHECK (Email LIKE '%_@_%._%')
        );
        
        PRINT 'Table [auth].[Users] created successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Table [auth].[Users] already exists. Skipping creation.';
    END

    -- Add foreign key constraints after both tables exist
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Users_Roles')
    BEGIN
        ALTER TABLE [auth].[Users] ADD CONSTRAINT FK_Users_Roles 
            FOREIGN KEY (RoleId) REFERENCES [auth].[Roles](RoleId);
        PRINT 'Added FK_Users_Roles constraint.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Users_CreatedBy')
    BEGIN
        ALTER TABLE [auth].[Users] ADD CONSTRAINT FK_Users_CreatedBy 
            FOREIGN KEY (CreatedBy) REFERENCES [auth].[Users](UserId);
        PRINT 'Added FK_Users_CreatedBy constraint.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Users_ModifiedBy')
    BEGIN
        ALTER TABLE [auth].[Users] ADD CONSTRAINT FK_Users_ModifiedBy 
            FOREIGN KEY (ModifiedBy) REFERENCES [auth].[Users](UserId);
        PRINT 'Added FK_Users_ModifiedBy constraint.';
    END

    -- =============================================
    -- Table: UserSessions
    -- =============================================
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserSessions' AND schema_id = SCHEMA_ID('auth'))
    BEGIN
        PRINT 'Creating table [auth].[UserSessions]...';
        
        CREATE TABLE [auth].[UserSessions] (
            SessionId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
            UserId UNIQUEIDENTIFIER NOT NULL,
            SessionToken NVARCHAR(500) NOT NULL,
            RefreshToken NVARCHAR(500) NULL,
            IPAddress NVARCHAR(45) NOT NULL,
            UserAgent NVARCHAR(500) NULL,
            CreatedDate DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
            ExpiryDate DATETIME2(7) NOT NULL,
            RefreshTokenExpiryDate DATETIME2(7) NULL,
            IsActive BIT NOT NULL DEFAULT 1,
            RevokedDate DATETIME2(7) NULL,
            RevokedBy UNIQUEIDENTIFIER NULL,
            RevokedReason NVARCHAR(500) NULL,
            
            CONSTRAINT PK_UserSessions PRIMARY KEY CLUSTERED (SessionId),
            CONSTRAINT FK_UserSessions_Users FOREIGN KEY (UserId) REFERENCES [auth].[Users](UserId),
            CONSTRAINT FK_UserSessions_RevokedBy FOREIGN KEY (RevokedBy) REFERENCES [auth].[Users](UserId)
        );
        
        PRINT 'Table [auth].[UserSessions] created successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Table [auth].[UserSessions] already exists. Skipping creation.';
    END

    -- =============================================
    -- Table: PasswordHistory
    -- =============================================
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PasswordHistory' AND schema_id = SCHEMA_ID('auth'))
    BEGIN
        PRINT 'Creating table [auth].[PasswordHistory]...';
        
        CREATE TABLE [auth].[PasswordHistory] (
            PasswordHistoryId INT IDENTITY(1,1) NOT NULL,
            UserId UNIQUEIDENTIFIER NOT NULL,
            PasswordHash NVARCHAR(500) NOT NULL,
            CreatedDate DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
            
            CONSTRAINT PK_PasswordHistory PRIMARY KEY CLUSTERED (PasswordHistoryId),
            CONSTRAINT FK_PasswordHistory_Users FOREIGN KEY (UserId) REFERENCES [auth].[Users](UserId)
        );
        
        PRINT 'Table [auth].[PasswordHistory] created successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Table [auth].[PasswordHistory] already exists. Skipping creation.';
    END

    -- =============================================
    -- Table: LoginAttempts
    -- =============================================
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LoginAttempts' AND schema_id = SCHEMA_ID('auth'))
    BEGIN
        PRINT 'Creating table [auth].[LoginAttempts]...';
        
        CREATE TABLE [auth].[LoginAttempts] (
            AttemptId BIGINT IDENTITY(1,1) NOT NULL,
            Username NVARCHAR(100) NOT NULL,
            IPAddress NVARCHAR(45) NOT NULL,
            UserAgent NVARCHAR(500) NULL,
            IsSuccessful BIT NOT NULL,
            FailureReason NVARCHAR(500) NULL,
            AttemptDate DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
            
            CONSTRAINT PK_LoginAttempts PRIMARY KEY CLUSTERED (AttemptId)
        );
        
        PRINT 'Table [auth].[LoginAttempts] created successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Table [auth].[LoginAttempts] already exists. Skipping creation.';
    END

    COMMIT TRANSACTION;
    PRINT 'User tables transaction committed successfully.';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'ERROR in user tables creation:';
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS NVARCHAR(10));
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Line Number: ' + CAST(ERROR_LINE() AS NVARCHAR(10));
    RETURN;
END CATCH

-- =============================================
-- Create Indexes (safe to run multiple times)
-- =============================================
BEGIN TRY
    PRINT 'Creating indexes for user tables...';

    -- Users table indexes
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Username' AND object_id = OBJECT_ID('[auth].[Users]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Users_Username 
        ON [auth].[Users](Username) 
        INCLUDE (UserId, PasswordHash, PasswordSalt, IsActive, IsLocked);
        PRINT 'Created index IX_Users_Username.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Email' AND object_id = OBJECT_ID('[auth].[Users]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Users_Email 
        ON [auth].[Users](Email) 
        INCLUDE (UserId, Username, IsEmailVerified);
        PRINT 'Created index IX_Users_Email.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_RoleId' AND object_id = OBJECT_ID('[auth].[Users]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Users_RoleId 
        ON [auth].[Users](RoleId) 
        INCLUDE (Username, Email, IsActive);
        PRINT 'Created index IX_Users_RoleId.';
    END

    -- UserSessions table indexes
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserSessions_UserId' AND object_id = OBJECT_ID('[auth].[UserSessions]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_UserSessions_UserId 
        ON [auth].[UserSessions](UserId, IsActive) 
        INCLUDE (SessionToken, ExpiryDate);
        PRINT 'Created index IX_UserSessions_UserId.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserSessions_SessionToken' AND object_id = OBJECT_ID('[auth].[UserSessions]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_UserSessions_SessionToken 
        ON [auth].[UserSessions](SessionToken) 
        INCLUDE (UserId, ExpiryDate, IsActive);
        PRINT 'Created index IX_UserSessions_SessionToken.';
    END

    -- LoginAttempts table indexes
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LoginAttempts_Username_Date' AND object_id = OBJECT_ID('[auth].[LoginAttempts]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_LoginAttempts_Username_Date 
        ON [auth].[LoginAttempts](Username, AttemptDate DESC) 
        INCLUDE (IsSuccessful, IPAddress);
        PRINT 'Created index IX_LoginAttempts_Username_Date.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LoginAttempts_IPAddress_Date' AND object_id = OBJECT_ID('[auth].[LoginAttempts]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_LoginAttempts_IPAddress_Date 
        ON [auth].[LoginAttempts](IPAddress, AttemptDate DESC) 
        INCLUDE (Username, IsSuccessful);
        PRINT 'Created index IX_LoginAttempts_IPAddress_Date.';
    END

    -- PasswordHistory table indexes
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PasswordHistory_UserId' AND object_id = OBJECT_ID('[auth].[PasswordHistory]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_PasswordHistory_UserId 
        ON [auth].[PasswordHistory](UserId, CreatedDate DESC);
        PRINT 'Created index IX_PasswordHistory_UserId.';
    END

    PRINT 'User table indexes created/verified successfully.';

END TRY
BEGIN CATCH
    PRINT 'ERROR creating indexes:';
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS NVARCHAR(10));
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    -- Continue anyway as indexes are not critical for basic functionality
END CATCH

PRINT 'User management tables setup completed successfully.';
GO
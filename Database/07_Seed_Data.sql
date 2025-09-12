-- =============================================
-- Seed Data for Audit Trail System
-- Includes: Roles, Permissions, Configuration
-- Re-runnable: YES
-- =============================================

SET NOCOUNT ON;
USE [AuditTrail];
GO

PRINT 'Starting seed data insertion...';

-- =============================================
-- Insert the 14 fixed roles
-- =============================================
BEGIN TRY
    PRINT 'Inserting roles...';

    -- Check if roles already exist
    IF NOT EXISTS (SELECT 1 FROM [auth].[Roles] WHERE RoleName = 'Site Admin')
    BEGIN
        INSERT INTO [auth].[Roles] (RoleName, Description, IsActive)
        VALUES 
            ('Site Admin', 'Full system administration privileges', 1),
            ('Study Coordinator', 'Manages study documents and participants', 1),
            ('Study Investigator', 'Principal investigator with document access', 1),
            ('Unblinded Study Staff', 'Staff with access to unblinded data', 1),
            ('Blinded Monitor', 'Monitor with restricted access to blinded data only', 1),
            ('Unblinded Monitor', 'Monitor with full data access', 1),
            ('Sponsor Support', 'Sponsor representative with document access', 1),
            ('Auditor', 'Internal or external auditor with read-only access', 1),
            ('Blinded Archivist', 'Manages archived blinded documents', 1),
            ('Unblinded Archivist', 'Manages all archived documents', 1),
            ('Binder Setup Blinded', 'Sets up document binders with blinded access', 1),
            ('Quality Control', 'QC team with document review access', 1),
            ('System Support', 'Technical support with limited admin access', 1),
            ('System Team Setup', 'Initial system configuration team', 1);
        
        PRINT 'Roles inserted successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Roles already exist. Skipping insertion.';
    END
END TRY
BEGIN CATCH
    PRINT 'ERROR inserting roles: ' + ERROR_MESSAGE();
END CATCH

-- =============================================
-- Insert base permissions
-- =============================================
BEGIN TRY
    PRINT 'Inserting permissions...';

    -- Check if permissions already exist
    IF NOT EXISTS (SELECT 1 FROM [auth].[Permissions] WHERE PermissionCode = 'FILE_VIEW')
    BEGIN
        INSERT INTO [auth].[Permissions] (PermissionName, PermissionCode, ResourceType, Description)
        VALUES 
            -- File permissions
            ('View Files', 'FILE_VIEW', 'File', 'View file metadata and listings'),
            ('Download Files', 'FILE_DOWNLOAD', 'File', 'Download files to local system'),
            ('Upload Files', 'FILE_UPLOAD', 'File', 'Upload new files or versions'),
            ('Delete Files', 'FILE_DELETE', 'File', 'Soft delete files'),
            ('Modify File Metadata', 'FILE_MODIFY', 'File', 'Edit file properties and metadata'),
            ('Archive Files', 'FILE_ARCHIVE', 'File', 'Archive and restore files'),
            
            -- User management permissions
            ('View Users', 'USER_VIEW', 'User', 'View user profiles and lists'),
            ('Create Users', 'USER_CREATE', 'User', 'Create new user accounts'),
            ('Modify Users', 'USER_MODIFY', 'User', 'Edit user profiles and settings'),
            ('Delete Users', 'USER_DELETE', 'User', 'Deactivate user accounts'),
            ('Reset Passwords', 'USER_RESET_PWD', 'User', 'Reset user passwords'),
            ('Unlock Accounts', 'USER_UNLOCK', 'User', 'Unlock locked user accounts'),
            
            -- Audit permissions
            ('View Audit Trail', 'AUDIT_VIEW', 'System', 'View audit log entries'),
            ('Export Audit Trail', 'AUDIT_EXPORT', 'System', 'Export audit logs to files'),
            ('Generate Reports', 'AUDIT_REPORT', 'System', 'Generate compliance reports'),
            
            -- System permissions
            ('System Configuration', 'SYSTEM_CONFIG', 'System', 'Modify system settings'),
            ('Manage Roles', 'SYSTEM_ROLES', 'System', 'Create and modify roles'),
            ('Manage Permissions', 'SYSTEM_PERMS', 'System', 'Assign permissions to roles'),
            ('Backup System', 'SYSTEM_BACKUP', 'System', 'Create system backups'),
            ('View Logs', 'SYSTEM_LOGS', 'System', 'View system logs'),
            
            -- Report permissions
            ('View Reports', 'REPORT_VIEW', 'Report', 'View generated reports'),
            ('Create Reports', 'REPORT_CREATE', 'Report', 'Generate new reports'),
            ('Export Reports', 'REPORT_EXPORT', 'Report', 'Export reports to various formats');
        
        PRINT 'Permissions inserted successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Permissions already exist. Skipping insertion.';
    END
END TRY
BEGIN CATCH
    PRINT 'ERROR inserting permissions: ' + ERROR_MESSAGE();
END CATCH

-- =============================================
-- Insert audit event types
-- =============================================
BEGIN TRY
    PRINT 'Inserting audit event types...';

    -- Check if audit event types already exist
    IF NOT EXISTS (SELECT 1 FROM [audit].[AuditEventTypes] WHERE EventType = 'USER_LOGIN')
    BEGIN
        INSERT INTO [audit].[AuditEventTypes] (EventCategory, EventType, EventDescription, Severity)
        VALUES 
            -- User events
            ('User', 'USER_LOGIN', 'User logged in successfully', 'Info'),
            ('User', 'USER_LOGOUT', 'User logged out', 'Info'),
            ('User', 'USER_LOGIN_FAILED', 'Failed login attempt', 'Warning'),
            ('User', 'USER_LOCKED', 'User account locked', 'Warning'),
            ('User', 'USER_UNLOCKED', 'User account unlocked', 'Info'),
            ('User', 'USER_CREATED', 'New user account created', 'Info'),
            ('User', 'USER_MODIFIED', 'User account modified', 'Info'),
            ('User', 'USER_DEACTIVATED', 'User account deactivated', 'Warning'),
            ('User', 'PASSWORD_CHANGED', 'User password changed', 'Info'),
            ('User', 'PASSWORD_RESET', 'User password reset', 'Info'),
            
            -- File events
            ('File', 'FILE_UPLOADED', 'File uploaded successfully', 'Info'),
            ('File', 'FILE_VIEWED', 'File viewed', 'Info'),
            ('File', 'FILE_DOWNLOADED', 'File downloaded', 'Info'),
            ('File', 'FILE_DELETED', 'File deleted', 'Warning'),
            ('File', 'FILE_RENAMED', 'File renamed', 'Info'),
            ('File', 'FILE_VERSION_CREATED', 'New file version created', 'Info'),
            ('File', 'FILE_METADATA_UPDATED', 'File metadata updated', 'Info'),
            ('File', 'FILE_ACCESS_DENIED', 'File access denied', 'Warning'),
            ('File', 'FILE_ARCHIVED', 'File archived', 'Info'),
            ('File', 'FILE_RESTORED', 'File restored from archive', 'Info'),
            
            -- System events
            ('System', 'SYSTEM_STARTUP', 'System started', 'Info'),
            ('System', 'SYSTEM_SHUTDOWN', 'System shut down', 'Info'),
            ('System', 'BACKUP_CREATED', 'System backup created', 'Info'),
            ('System', 'CONFIG_CHANGED', 'System configuration changed', 'Warning'),
            ('System', 'ROLE_CREATED', 'New role created', 'Info'),
            ('System', 'ROLE_MODIFIED', 'Role modified', 'Info'),
            ('System', 'PERMISSION_GRANTED', 'Permission granted', 'Info'),
            ('System', 'PERMISSION_REVOKED', 'Permission revoked', 'Warning'),
            
            -- Security events
            ('Security', 'SUSPICIOUS_ACTIVITY', 'Suspicious activity detected', 'Critical'),
            ('Security', 'BRUTE_FORCE_ATTEMPT', 'Brute force attack detected', 'Critical'),
            ('Security', 'UNAUTHORIZED_ACCESS', 'Unauthorized access attempt', 'Critical'),
            ('Security', 'SESSION_HIJACK_ATTEMPT', 'Session hijacking attempt', 'Critical');
        
        PRINT 'Audit event types inserted successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Audit event types already exist. Skipping insertion.';
    END
END TRY
BEGIN CATCH
    PRINT 'ERROR inserting audit event types: ' + ERROR_MESSAGE();
END CATCH

-- =============================================
-- Create system user and insert configuration
-- =============================================
BEGIN TRY
    PRINT 'Creating system user and inserting configuration...';

    DECLARE @SystemUserId UNIQUEIDENTIFIER;
    
    -- Check if system user already exists
    SELECT @SystemUserId = UserId FROM [auth].[Users] WHERE Username = 'system';
    
    IF @SystemUserId IS NULL
    BEGIN
        SET @SystemUserId = NEWID();
        INSERT INTO [auth].[Users] (
            UserId, Username, Email, PasswordHash, PasswordSalt, 
            FirstName, LastName, RoleId, IsActive, IsEmailVerified
        )
        VALUES (
            @SystemUserId, 
            'system', 
            'system@audittrail.local',
            'SYSTEM_ACCOUNT_NO_LOGIN',
            'SYSTEM_ACCOUNT_NO_LOGIN',
            'System',
            'Administrator',
            (SELECT RoleId FROM [auth].[Roles] WHERE RoleName = 'Site Admin'),
            1,
            1
        );
        PRINT 'System user created successfully.';
    END
    ELSE
    BEGIN
        PRINT 'System user already exists.';
    END

    -- Insert system configuration if not exists
    IF NOT EXISTS (SELECT 1 FROM [config].[SystemConfiguration] WHERE ConfigKey = 'PasswordMinLength')
    BEGIN
        INSERT INTO [config].[SystemConfiguration] (ConfigCategory, ConfigKey, ConfigValue, ConfigType, Description, LastModifiedBy)
        VALUES 
            ('Security', 'PasswordMinLength', '12', 'Integer', 'Minimum password length', @SystemUserId),
            ('Security', 'PasswordRequireUppercase', 'true', 'Boolean', 'Require uppercase letters in password', @SystemUserId),
            ('Security', 'PasswordRequireLowercase', 'true', 'Boolean', 'Require lowercase letters in password', @SystemUserId),
            ('Security', 'PasswordRequireDigit', 'true', 'Boolean', 'Require digits in password', @SystemUserId),
            ('Security', 'PasswordRequireSpecial', 'true', 'Boolean', 'Require special characters in password', @SystemUserId),
            ('Security', 'MaxLoginAttempts', '5', 'Integer', 'Maximum login attempts before lockout', @SystemUserId),
            ('Security', 'LockoutDurationMinutes', '0', 'Integer', 'Lockout duration (0 = until admin unlock)', @SystemUserId),
            ('Security', 'SessionTimeoutMinutes', '30', 'Integer', 'Session timeout in minutes', @SystemUserId),
            ('Security', 'PasswordExpiryDays', '90', 'Integer', 'Password expiry in days', @SystemUserId),
            ('Security', 'PasswordHistoryCount', '5', 'Integer', 'Number of previous passwords to check', @SystemUserId),
            
            ('Storage', 'MaxFileSize', '52428800', 'Integer', 'Maximum file size in bytes (50MB)', @SystemUserId),
            ('Storage', 'AllowedFileExtensions', '.pdf,.doc,.docx', 'String', 'Allowed file extensions', @SystemUserId),
            ('Storage', 'S3BucketName', 'audit-trail-documents', 'String', 'AWS S3 bucket name', @SystemUserId),
            ('Storage', 'S3Region', 'us-east-1', 'String', 'AWS S3 region', @SystemUserId),
            ('Storage', 'EnableEncryption', 'true', 'Boolean', 'Enable file encryption', @SystemUserId),
            
            ('Audit', 'RetentionDays', '365', 'Integer', 'Audit log retention in days', @SystemUserId),
            ('Audit', 'ArchiveAfterDays', '180', 'Integer', 'Archive audit logs after days', @SystemUserId),
            ('Audit', 'EnableRealTimeAudit', 'true', 'Boolean', 'Enable real-time audit logging', @SystemUserId),
            
            ('System', 'ApplicationName', 'Audit Trail System', 'String', 'Application name', @SystemUserId),
            ('System', 'ApplicationVersion', '1.0.0', 'String', 'Application version', @SystemUserId),
            ('System', 'MaintenanceMode', 'false', 'Boolean', 'System maintenance mode', @SystemUserId),
            ('System', 'DefaultTimeZone', 'UTC', 'String', 'Default system timezone', @SystemUserId);
        
        PRINT 'System configuration inserted successfully.';
    END
    ELSE
    BEGIN
        PRINT 'System configuration already exists. Skipping insertion.';
    END

    -- Insert retention policies
    IF NOT EXISTS (SELECT 1 FROM [config].[RetentionPolicies] WHERE PolicyName = 'Audit Log Retention')
    BEGIN
        INSERT INTO [config].[RetentionPolicies] (PolicyName, EntityType, RetentionDays, ArchiveAfterDays, DeleteAfterDays, CreatedBy)
        VALUES 
            ('Audit Log Retention', 'AuditLog', 365, 180, NULL, @SystemUserId),
            ('File Retention', 'File', 2555, 365, NULL, @SystemUserId),
            ('Session Retention', 'UserSession', 30, NULL, 30, @SystemUserId),
            ('Login Attempt Retention', 'LoginAttempt', 90, NULL, 90, @SystemUserId);
        
        PRINT 'Retention policies inserted successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Retention policies already exist. Skipping insertion.';
    END

    -- Insert scheduled jobs
    IF NOT EXISTS (SELECT 1 FROM [config].[ScheduledJobs] WHERE JobName = 'Archive Old Audit Logs')
    BEGIN
        INSERT INTO [config].[ScheduledJobs] (JobName, JobType, JobDescription, FrequencyMinutes, IsEnabled, Configuration, CreatedBy)
        VALUES 
            ('Archive Old Audit Logs', 'Archive', 'Archives audit logs older than 180 days', 1440, 1,
             '{"SourceTable":"audit.AuditTrail","TargetTable":"audit.AuditTrailArchive","DaysOld":180}',
             @SystemUserId),
            ('Clean Expired Sessions', 'Cleanup', 'Removes expired user sessions', 60, 1,
             '{"Table":"auth.UserSessions","ExpiryField":"ExpiryDate"}',
             @SystemUserId),
            ('Generate Daily Audit Summary', 'Report', 'Creates daily audit summary report', 1440, 1,
             '{"ReportType":"AuditSummary","Recipients":["admin@audittrail.local"]}',
             @SystemUserId),
            ('Database Backup', 'Backup', 'Full database backup', 1440, 1,
             '{"BackupType":"Full","Compression":true,"Encryption":true}',
             @SystemUserId),
            ('File Storage Cleanup', 'Cleanup', 'Removes orphaned files from storage', 10080, 1,
             '{"CheckOrphaned":true,"CheckCorrupted":true}',
             @SystemUserId);
        
        PRINT 'Scheduled jobs inserted successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Scheduled jobs already exist. Skipping insertion.';
    END

END TRY
BEGIN CATCH
    PRINT 'ERROR in system configuration: ' + ERROR_MESSAGE();
END CATCH

-- =============================================
-- Create initial admin user
-- =============================================
BEGIN TRY
    PRINT 'Creating initial admin user...';

    DECLARE @AdminUserId UNIQUEIDENTIFIER;
    
    -- Check if admin user already exists
    SELECT @AdminUserId = UserId FROM [auth].[Users] WHERE Username = 'admin';
    
    IF @AdminUserId IS NULL
    BEGIN
        SET @AdminUserId = NEWID();
        INSERT INTO [auth].[Users] (
            UserId, Username, Email, 
            PasswordHash,
            PasswordSalt,
            FirstName, LastName, RoleId, 
            IsActive, IsEmailVerified, MustChangePassword
        )
        VALUES (
            @AdminUserId,
            'admin',
            'admin@audittrail.local',
            'TEMP_HASH_CHANGE_ON_FIRST_LOGIN',
            'TEMP_SALT_CHANGE_ON_FIRST_LOGIN',
            'System',
            'Administrator',
            (SELECT RoleId FROM [auth].[Roles] WHERE RoleName = 'Site Admin'),
            1,
            1,
            1
        );
        PRINT 'Admin user created successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Admin user already exists.';
    END

    -- Assign permissions to Site Admin role
    IF NOT EXISTS (SELECT 1 FROM [auth].[RolePermissions] WHERE RoleId = (SELECT RoleId FROM [auth].[Roles] WHERE RoleName = 'Site Admin'))
    BEGIN
        DECLARE @SiteAdminRoleId INT = (SELECT RoleId FROM [auth].[Roles] WHERE RoleName = 'Site Admin');
        
        INSERT INTO [auth].[RolePermissions] (RoleId, PermissionId, ResourceType, AccessLevel, GrantedBy)
        SELECT 
            @SiteAdminRoleId,
            PermissionId,
            ResourceType,
            63,
            @AdminUserId
        FROM [auth].[Permissions];
        
        PRINT 'Permissions assigned to Site Admin role successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Site Admin permissions already exist.';
    END

    -- Assign limited permissions to Auditor role
    IF NOT EXISTS (SELECT 1 FROM [auth].[RolePermissions] WHERE RoleId = (SELECT RoleId FROM [auth].[Roles] WHERE RoleName = 'Auditor'))
    BEGIN
        DECLARE @AuditorRoleId INT = (SELECT RoleId FROM [auth].[Roles] WHERE RoleName = 'Auditor');
        
        INSERT INTO [auth].[RolePermissions] (RoleId, PermissionId, ResourceType, AccessLevel, GrantedBy)
        SELECT 
            @AuditorRoleId,
            PermissionId,
            ResourceType,
            3,
            @AdminUserId
        FROM [auth].[Permissions]
        WHERE PermissionCode IN ('FILE_VIEW', 'FILE_DOWNLOAD', 'AUDIT_VIEW', 'AUDIT_EXPORT', 'REPORT_VIEW');
        
        PRINT 'Permissions assigned to Auditor role successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Auditor permissions already exist.';
    END

END TRY
BEGIN CATCH
    PRINT 'ERROR creating admin user or assigning permissions: ' + ERROR_MESSAGE();
END CATCH

PRINT 'Seed data insertion completed successfully';

-- =============================================
-- Display summary
-- =============================================
BEGIN TRY
    SELECT 'Summary Report' as [Report];
    
    SELECT 'Roles' as [Table], COUNT(*) as [Count] FROM [auth].[Roles]
    UNION ALL
    SELECT 'Permissions', COUNT(*) FROM [auth].[Permissions]
    UNION ALL
    SELECT 'Users', COUNT(*) FROM [auth].[Users]
    UNION ALL
    SELECT 'Audit Event Types', COUNT(*) FROM [audit].[AuditEventTypes]
    UNION ALL
    SELECT 'System Configuration', COUNT(*) FROM [config].[SystemConfiguration]
    UNION ALL
    SELECT 'Scheduled Jobs', COUNT(*) FROM [config].[ScheduledJobs];
    
    PRINT 'Summary report generated successfully.';
END TRY
BEGIN CATCH
    PRINT 'ERROR generating summary: ' + ERROR_MESSAGE();
END CATCH
GO
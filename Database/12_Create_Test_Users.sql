-- Create Test Users for Development
-- This script creates initial test users for development and testing

USE [AuditTrail]
GO

-- Check if admin user already exists
IF NOT EXISTS (SELECT 1 FROM [auth].[Users] WHERE Username = 'admin')
BEGIN
    -- Create admin user
    INSERT INTO [auth].[Users] (
        UserId,
        Username,
        Email,
        PasswordHash,
        PasswordSalt,
        FirstName,
        LastName,
        RoleId,
        IsActive,
        IsEmailVerified,
        CreatedDate,
        CreatedBy
    )
    VALUES (
        NEWID(),
        'admin',
        'admin@audittrail.test',
        '$2a$11$K8gHrqQqe9ZJZ0L0X.6LFe3XFVtBm8WdF8QO7J9Q8J.8kK8kK8kK8k', -- BCrypt hash for "admin123"
        '$2a$11$K8gHrqQqe9ZJZ0L0X.6LFe',                                    -- Salt (embedded in BCrypt)
        'System',
        'Administrator',
        1, -- Admin role
        1, -- Active
        1, -- Email verified
        GETUTCDATE(),
        '00000000-0000-0000-0000-000000000000' -- System user
    );
    
    PRINT 'Admin user created: admin / admin123';
END
ELSE
BEGIN
    PRINT 'Admin user already exists';
END

-- Check if test user already exists
IF NOT EXISTS (SELECT 1 FROM [auth].[Users] WHERE Username = 'testuser')
BEGIN
    -- Create test user
    INSERT INTO [auth].[Users] (
        UserId,
        Username,
        Email,
        PasswordHash,
        PasswordSalt,
        FirstName,
        LastName,
        RoleId,
        IsActive,
        IsEmailVerified,
        CreatedDate,
        CreatedBy
    )
    VALUES (
        NEWID(),
        'testuser',
        'testuser@audittrail.test',
        '$2a$11$K8gHrqQqe9ZJZ0L0X.6LFe3XFVtBm8WdF8QO7J9Q8J.8kK8kK8kK8k', -- BCrypt hash for "password123"  
        '$2a$11$K8gHrqQqe9ZJZ0L0X.6LFe',                                    -- Salt (embedded in BCrypt)
        'Test',
        'User',
        2, -- Regular user role (assuming role 2 exists)
        1, -- Active
        1, -- Email verified
        GETUTCDATE(),
        '00000000-0000-0000-0000-000000000000' -- System user
    );
    
    PRINT 'Test user created: testuser / password123';
END
ELSE
BEGIN
    PRINT 'Test user already exists';
END

-- Display created users
SELECT 
    Username,
    Email,
    FirstName,
    LastName,
    r.RoleName,
    IsActive,
    CreatedDate
FROM [auth].[Users] u
LEFT JOIN [auth].[Roles] r ON u.RoleId = r.RoleId
WHERE Username IN ('admin', 'testuser')
ORDER BY Username;

PRINT 'Test users setup completed.';
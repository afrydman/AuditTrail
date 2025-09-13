-- Create Test Users for Development
-- This script uses the SetupController or simple user creation

USE [AuditTrail]
GO

-- Check current users
SELECT 
    u.Username,
    u.Email,
    u.FirstName,
    u.LastName,
    r.RoleName,
    u.IsActive,
    u.CreatedDate
FROM [auth].[Users] u
LEFT JOIN [auth].[Roles] r ON u.RoleId = r.RoleId
ORDER BY u.CreatedDate DESC;

-- Check if any users exist
DECLARE @UserCount INT;
SELECT @UserCount = COUNT(*) FROM [auth].[Users];

IF @UserCount = 0
BEGIN
    PRINT 'No users found. Please use the API SetupController to create the first admin user:';
    PRINT '';
    PRINT 'POST /api/setup/create-admin';
    PRINT 'Content-Type: application/json';
    PRINT '';  
    PRINT '{';
    PRINT '  "username": "admin",';
    PRINT '  "email": "admin@audittrail.test",';
    PRINT '  "firstName": "System",';
    PRINT '  "lastName": "Administrator",';
    PRINT '  "password": "admin123"';
    PRINT '}';
    PRINT '';
    PRINT 'Then create a test user:';
    PRINT 'POST /api/setup/create-test-user';
    PRINT '{';
    PRINT '  "username": "testuser",';
    PRINT '  "email": "testuser@audittrail.test",';
    PRINT '  "firstName": "Test",';
    PRINT '  "lastName": "User",';
    PRINT '  "password": "password123",';
    PRINT '  "roleId": 2';
    PRINT '}';
END
ELSE
BEGIN
    PRINT CAST(@UserCount AS VARCHAR(10)) + ' user(s) already exist in the system.';
END

PRINT 'Script completed.';
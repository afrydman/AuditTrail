-- Fix admin password with proper BCrypt hash
USE [AuditTrail]
GO

UPDATE [auth].[Users] 
SET PasswordHash = '$2a$11$.gYR1V/nyTae4Q9KDlLgxekEp2yNi9j8Ik/hsixB15GEyAAuomuJa', 
    PasswordSalt = ''
WHERE Username = 'admin';

-- Verify the update
SELECT 
    Username, 
    LEN(PasswordHash) as HashLength,
    LEFT(PasswordHash, 10) + '...' as HashPreview
FROM [auth].[Users] 
WHERE Username = 'admin';

PRINT 'Admin password updated. Credentials: admin / admin123';
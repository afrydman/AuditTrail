-- Fix system password with proper BCrypt hash  
USE [AuditTrail]
GO

UPDATE [auth].[Users] 
SET PasswordHash = '$2a$11$wJ1v3snu0sKs4ww3SPNyBuicCU/TjNo10DtsgLRVOilT8vNfoVQgO', 
    PasswordSalt = ''
WHERE Username = 'system';

-- Verify both users
SELECT 
    Username, 
    LEN(PasswordHash) as HashLength,
    LEFT(PasswordHash, 10) + '...' as HashPreview
FROM [auth].[Users] 
WHERE Username IN ('admin', 'system')
ORDER BY Username;

PRINT 'Test credentials updated:';
PRINT 'admin / admin123';
PRINT 'system / system123';
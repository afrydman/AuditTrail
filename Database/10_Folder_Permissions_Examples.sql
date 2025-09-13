-- =============================================
-- Folder Permission Examples
-- Shows how to implement common permission scenarios
-- =============================================

USE [AuditTrail];
GO

-- =============================================
-- SCENARIO 1: Study Folder Structure with Role-Based Access
-- =============================================
/*
Folder Structure:
/Studies/
├── STUDY001/
│   ├── Protocol/
│   │   ├── Amendments/
│   │   └── Approvals/
│   ├── Data/
│   │   ├── Raw/
│   │   └── Processed/
│   └── Reports/
│       ├── Monthly/
│       └── Final/
*/

-- Create the folder structure
DECLARE @SystemUserId UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000000';

-- Root Studies folder
INSERT INTO [docs].[FileCategories] (CategoryName, CategoryPath, ParentCategoryId, CreatedBy)
VALUES ('Studies', '/Studies', NULL, @SystemUserId);

DECLARE @StudiesId INT = SCOPE_IDENTITY();

-- STUDY001 folder
INSERT INTO [docs].[FileCategories] (CategoryName, CategoryPath, ParentCategoryId, CreatedBy)
VALUES ('STUDY001', '/Studies/STUDY001', @StudiesId, @SystemUserId);

DECLARE @Study001Id INT = SCOPE_IDENTITY();

-- Protocol subfolder
INSERT INTO [docs].[FileCategories] (CategoryName, CategoryPath, ParentCategoryId, CreatedBy)
VALUES ('Protocol', '/Studies/STUDY001/Protocol', @Study001Id, @SystemUserId);

DECLARE @ProtocolId INT = SCOPE_IDENTITY();

-- =============================================
-- PERMISSION EXAMPLES
-- =============================================

-- Example 1: Grant Study Coordinator full access to entire study folder (with inheritance)
DECLARE @StudyCoordinatorRoleId INT = (SELECT RoleId FROM [auth].[Roles] WHERE RoleName = 'Study Coordinator');

INSERT INTO [docs].[CategoryAccess] 
    (CategoryId, RoleId, Permissions, InheritToSubfolders, InheritToFiles, GrantedBy)
VALUES 
    (@Study001Id, @StudyCoordinatorRoleId, 63, 1, 1, @SystemUserId);
-- Result: Study Coordinators can do everything in STUDY001 and all subfolders

-- Example 2: Grant Auditors read-only access to entire Studies folder
DECLARE @AuditorRoleId INT = (SELECT RoleId FROM [auth].[Roles] WHERE RoleName = 'Auditor');

INSERT INTO [docs].[CategoryAccess] 
    (CategoryId, RoleId, Permissions, InheritToSubfolders, InheritToFiles, GrantedBy)
VALUES 
    (@StudiesId, @AuditorRoleId, 3, 1, 1, @SystemUserId);
-- Result: Auditors can view and download all files in all studies

-- Example 3: Grant specific user upload rights to Protocol folder only (no inheritance)
DECLARE @SpecificUserId UNIQUEIDENTIFIER = 'USER-GUID-HERE';

INSERT INTO [docs].[CategoryAccess] 
    (CategoryId, UserId, Permissions, InheritToSubfolders, InheritToFiles, GrantedBy, ExpiryDate)
VALUES 
    (@ProtocolId, @SpecificUserId, 7, 0, 1, @SystemUserId, DATEADD(day, 30, GETUTCDATE()));
-- Result: User can upload to Protocol folder for 30 days, but not to subfolders

-- Example 4: Deny access to Raw Data folder for Blinded roles
DECLARE @RawDataId INT = (SELECT CategoryId FROM [docs].[FileCategories] WHERE CategoryPath = '/Studies/STUDY001/Data/Raw');
DECLARE @BlindedMonitorRoleId INT = (SELECT RoleId FROM [auth].[Roles] WHERE RoleName = 'Blinded Monitor');

-- First, mark the folder as requiring explicit access
UPDATE [docs].[FileCategories] 
SET RequireExplicitAccess = 1 
WHERE CategoryId = @RawDataId;

-- Don't grant any permissions to blinded roles
-- Result: Even if they have parent folder access, they can't see Raw Data

-- =============================================
-- QUERY EXAMPLES: Check User Access
-- =============================================

-- Check what permissions a user has on a specific folder
DECLARE @UserToCheck UNIQUEIDENTIFIER = 'USER-GUID-HERE';
DECLARE @FolderToCheck INT = @ProtocolId;

EXEC [docs].[sp_CalculateEffectivePermissions] 
    @UserId = @UserToCheck,
    @ResourceType = 'Category',
    @ResourceId = @FolderToCheck;

-- List all folders a user can access
WITH UserRolePermissions AS (
    SELECT 
        fc.CategoryId,
        fc.CategoryPath,
        fc.CategoryName,
        ca.Permissions,
        'Role-based' AS AccessType
    FROM [docs].[FileCategories] fc
    INNER JOIN [docs].[CategoryAccess] ca ON fc.CategoryId = ca.CategoryId
    INNER JOIN [auth].[Users] u ON ca.RoleId = u.RoleId
    WHERE u.UserId = @UserToCheck
        AND ca.IsActive = 1
        AND (ca.ExpiryDate IS NULL OR ca.ExpiryDate > GETUTCDATE())
    
    UNION
    
    SELECT 
        fc.CategoryId,
        fc.CategoryPath,
        fc.CategoryName,
        ca.Permissions,
        'Direct' AS AccessType
    FROM [docs].[FileCategories] fc
    INNER JOIN [docs].[CategoryAccess] ca ON fc.CategoryId = ca.CategoryId
    WHERE ca.UserId = @UserToCheck
        AND ca.IsActive = 1
        AND (ca.ExpiryDate IS NULL OR ca.ExpiryDate > GETUTCDATE())
)
SELECT 
    CategoryPath,
    CategoryName,
    CASE 
        WHEN Permissions & 1 = 1 THEN 'Yes' ELSE 'No' 
    END AS CanView,
    CASE 
        WHEN Permissions & 2 = 2 THEN 'Yes' ELSE 'No' 
    END AS CanDownload,
    CASE 
        WHEN Permissions & 4 = 4 THEN 'Yes' ELSE 'No' 
    END AS CanUpload,
    CASE 
        WHEN Permissions & 8 = 8 THEN 'Yes' ELSE 'No' 
    END AS CanDelete,
    AccessType
FROM UserRolePermissions
ORDER BY CategoryPath;

-- =============================================
-- AUDIT TRAIL: Log folder access
-- =============================================

-- When user accesses a folder, log it
DECLARE @AuditId UNIQUEIDENTIFIER = NEWID();

INSERT INTO [audit].[AuditTrail] 
    (AuditId, EventType, EventCategory, UserId, Username, RoleName, 
     IPAddress, EntityType, EntityId, EntityName, Action, Result)
VALUES 
    (@AuditId, 'FolderAccess', 'Document', @UserToCheck, 'username', 'Study Coordinator',
     '192.168.1.100', 'Folder', @ProtocolId, '/Studies/STUDY001/Protocol', 'View', 'Success');

-- =============================================
-- COMMON PERMISSION PATTERNS
-- =============================================

-- Pattern 1: Department-based access
-- All users in a department get access to their department folder
DECLARE @DeptFolderId INT;
DECLARE @DeptRoleId INT;

INSERT INTO [docs].[CategoryAccess] 
    (CategoryId, RoleId, Permissions, InheritToSubfolders, InheritToFiles, GrantedBy)
VALUES 
    (@DeptFolderId, @DeptRoleId, 7, 1, 1, @SystemUserId);

-- Pattern 2: Time-limited contractor access
-- Contractor gets access only during project duration
INSERT INTO [docs].[CategoryAccess] 
    (CategoryId, UserId, Permissions, InheritToSubfolders, InheritToFiles, GrantedBy, ExpiryDate)
VALUES 
    (@Study001Id, 'CONTRACTOR-GUID', 3, 1, 1, @SystemUserId, '2025-12-31');

-- Pattern 3: Hierarchical approval workflow
-- Different access levels at different folder depths
-- Submitters: Upload to Drafts folder
-- Reviewers: Read Drafts, Upload to Reviewed folder  
-- Approvers: Full access to all folders

-- Pattern 4: Blinded study access
-- Create separate folder trees for blinded and unblinded content
/*
/Studies/STUDY001/
├── Blinded/     (Accessible to all study staff)
├── Unblinded/   (Restricted to unblinded roles only)
*/

-- =============================================
-- PERFORMANCE OPTIMIZATION: Refresh materialized permissions
-- =============================================

-- Job to refresh effective permissions cache (run periodically)
CREATE PROCEDURE [docs].[sp_RefreshEffectivePermissions]
AS
BEGIN
    -- Clear old cache entries
    DELETE FROM [docs].[EffectivePermissions] 
    WHERE LastCalculated < DATEADD(hour, -24, GETUTCDATE());
    
    -- Rebuild for active users who accessed system recently
    INSERT INTO [docs].[EffectivePermissions] 
        (ResourceType, ResourceId, UserId, Permissions, SourceType)
    SELECT DISTINCT
        'Category' AS ResourceType,
        fc.CategoryId AS ResourceId,
        u.UserId,
        dbo.fn_CalculatePermissions(u.UserId, fc.CategoryId) AS Permissions,
        'Calculated' AS SourceType
    FROM [auth].[Users] u
    CROSS JOIN [docs].[FileCategories] fc
    WHERE u.IsActive = 1
        AND u.LastLoginDate > DATEADD(day, -7, GETUTCDATE());
END
GO

PRINT 'Folder permission examples completed.';
PRINT 'These examples show common permission patterns for document management.';
GO
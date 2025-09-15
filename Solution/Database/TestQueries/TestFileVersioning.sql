-- Test queries to verify file versioning is working correctly
USE [AuditTrail];

-- Check all files with their versioning info
SELECT 
    FileId,
    FileName,
    Version,
    IsCurrentVersion,
    ParentFileId,
    CategoryId,
    IsDeleted,
    UploadedDate
FROM [docs].[Files]
ORDER BY FileName, Version DESC;

-- Check only current versions (what should appear in tree)
SELECT 
    FileId,
    FileName,
    Version,
    IsCurrentVersion,
    CategoryId,
    UploadedDate
FROM [docs].[Files]
WHERE IsDeleted = 0 AND IsCurrentVersion = 1
ORDER BY FileName;

-- Check if there are any files without IsCurrentVersion set
SELECT 
    FileId,
    FileName,
    Version,
    IsCurrentVersion
FROM [docs].[Files]
WHERE IsCurrentVersion IS NULL
ORDER BY FileName;
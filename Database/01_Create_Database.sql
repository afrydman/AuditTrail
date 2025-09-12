-- =============================================
-- Audit Trail System Database Creation Script
-- Database: AuditTrail
-- SQL Server 2019
-- CFR 21 Part 11 Compliant
-- Re-runnable: YES
-- =============================================

SET NOCOUNT ON;
GO


-- Set Database Options (safe to run multiple times)
BEGIN TRY
    PRINT 'Configuring database options...';
    
    -- Set database options
    IF EXISTS (SELECT 1 FROM sys.databases WHERE name = 'AuditTrail')
    BEGIN
        ALTER DATABASE [AuditTrail] SET COMPATIBILITY_LEVEL = 140; -- SQL Server 2019
        ALTER DATABASE [AuditTrail] SET RECOVERY FULL;
        ALTER DATABASE [AuditTrail] SET PAGE_VERIFY CHECKSUM;
        ALTER DATABASE [AuditTrail] SET AUTO_CREATE_STATISTICS ON;
        ALTER DATABASE [AuditTrail] SET AUTO_UPDATE_STATISTICS ON;
        ALTER DATABASE [AuditTrail] SET ALLOW_SNAPSHOT_ISOLATION ON;
        ALTER DATABASE [AuditTrail] SET READ_COMMITTED_SNAPSHOT ON;
        
        PRINT 'Database options configured successfully.';
    END
    
END TRY
BEGIN CATCH
    PRINT 'ERROR configuring database options:';
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS NVARCHAR(10));
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    -- Continue anyway as these are non-critical
END CATCH

-- Switch to the AuditTrail database
USE [AuditTrail];
GO

-- Create schemas (safe to run multiple times)
BEGIN TRY
    PRINT 'Creating schemas...';
    
    IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'auth')
        EXEC('CREATE SCHEMA [auth]');
    
    IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'docs')
        EXEC('CREATE SCHEMA [docs]');
    
    IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'audit')
        EXEC('CREATE SCHEMA [audit]');
    
    IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'config')
        EXEC('CREATE SCHEMA [config]');
    
    PRINT 'Schemas created/verified successfully.';
    
END TRY
BEGIN CATCH
    PRINT 'ERROR creating schemas:';
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS NVARCHAR(10));
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    RETURN;
END CATCH

PRINT 'Database setup completed successfully.';
PRINT 'Database: AuditTrail';
PRINT 'Schemas: auth, docs, audit, config';
GO
# Database Schema Documentation

## 🗄️ **Complete Database Schema (19 Tables)**

**Database**: AuditTrail  
**Schemas**: auth, docs, audit, config  
**Compliance**: CFR 21 Part 11 (FDA Electronic Records)

---

## 🔐 **auth Schema (8 Tables)**

### **1. Users**
**Purpose**: Core user accounts with authentication and profile data
```sql
CREATE TABLE [auth].[Users] (
    UserId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Username NVARCHAR(50) UNIQUE NOT NULL,
    Email NVARCHAR(255) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    Salt NVARCHAR(255) NOT NULL,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    IsLocked BIT NOT NULL DEFAULT 0,
    FailedLoginAttempts INT NOT NULL DEFAULT 0,
    LastLoginDate DATETIME2 NULL,
    PasswordChangedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy UNIQUEIDENTIFIER NOT NULL,
    ModifiedDate DATETIME2 NULL,
    ModifiedBy UNIQUEIDENTIFIER NULL
);
```
**Key Features**:
- BCrypt password hashing with salt
- Account lockout after failed attempts
- Audit trail of password changes
- Self-referencing CreatedBy/ModifiedBy

### **2. Roles**
**Purpose**: 14 predefined pharmaceutical/clinical research roles
```sql
CREATE TABLE [auth].[Roles] (
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(100) UNIQUE NOT NULL,
    Description NVARCHAR(500),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy UNIQUEIDENTIFIER NOT NULL,
    ModifiedDate DATETIME2 NULL,
    ModifiedBy UNIQUEIDENTIFIER NULL
);
```
**Fixed Roles Required**:
1. System Administrator
2. Study Manager  
3. Principal Investigator
4. Sub Investigator
5. Study Coordinator
6. Data Manager
7. Clinical Data Associate
8. Monitor
9. Quality Assurance
10. Regulatory Affairs
11. Biostatistician
12. Medical Writer
13. Pharmacovigilance
14. Audit User

### **3. UserSessions**
**Purpose**: Track active user sessions for security and audit
```sql
CREATE TABLE [auth].[UserSessions] (
    SessionId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    SessionToken NVARCHAR(255) NOT NULL,
    IpAddress NVARCHAR(45),
    UserAgent NVARCHAR(500),
    LoginTime DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastActivity DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ExpiresAt DATETIME2 NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (UserId) REFERENCES [auth].[Users](UserId)
);
```
**Features**:
- JWT token storage for API sessions
- Cookie session tracking for web
- IP address and browser tracking
- Concurrent session limits

### **4. PasswordHistory**
**Purpose**: Prevent password reuse (CFR 21 Part 11 requirement)
```sql
CREATE TABLE [auth].[PasswordHistory] (
    PasswordHistoryId INT IDENTITY(1,1) PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    Salt NVARCHAR(255) NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES [auth].[Users](UserId)
);
```
**Compliance**: Enforces password complexity and history rules

### **5. LoginAttempts**
**Purpose**: Security monitoring and account lockout
```sql
CREATE TABLE [auth].[LoginAttempts] (
    LoginAttemptId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL,
    IpAddress NVARCHAR(45),
    AttemptTime DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsSuccessful BIT NOT NULL,
    FailureReason NVARCHAR(255)
);
```
**Features**:
- Tracks both successful and failed attempts
- Supports account lockout policies
- IP-based brute force detection

### **6. Permissions**
**Purpose**: Define granular system permissions
```sql
CREATE TABLE [auth].[Permissions] (
    PermissionId INT IDENTITY(1,1) PRIMARY KEY,
    PermissionName NVARCHAR(100) UNIQUE NOT NULL,
    Description NVARCHAR(500),
    Category NVARCHAR(50),
    IsActive BIT NOT NULL DEFAULT 1
);
```
**Categories**:
- File Management (View, Download, Upload, Edit, Delete)
- User Management (Create, Modify, Deactivate)
- Audit Access (View, Export)
- System Administration (Configure, Backup)

### **7. RolePermissions** 
**Purpose**: Associate permissions with roles
```sql
CREATE TABLE [auth].[RolePermissions] (
    RolePermissionId INT IDENTITY(1,1) PRIMARY KEY,
    RoleId INT NOT NULL,
    PermissionId INT NOT NULL,
    GrantedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    GrantedBy UNIQUEIDENTIFIER NOT NULL,
    FOREIGN KEY (RoleId) REFERENCES [auth].[Roles](RoleId),
    FOREIGN KEY (PermissionId) REFERENCES [auth].[Permissions](PermissionId),
    UNIQUE (RoleId, PermissionId)
);
```

### **8. UserPermissions**
**Purpose**: Individual user permission overrides
```sql
CREATE TABLE [auth].[UserPermissions] (
    UserPermissionId INT IDENTITY(1,1) PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    PermissionId INT NOT NULL,
    IsGranted BIT NOT NULL, -- Can revoke role permissions
    GrantedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    GrantedBy UNIQUEIDENTIFIER NOT NULL,
    ExpiresAt DATETIME2 NULL,
    FOREIGN KEY (UserId) REFERENCES [auth].[Users](UserId),
    FOREIGN KEY (PermissionId) REFERENCES [auth].[Permissions](PermissionId),
    UNIQUE (UserId, PermissionId)
);
```

---

## 📁 **docs Schema (6 Tables)**

### **9. Files**
**Purpose**: Core document storage with metadata
```sql
CREATE TABLE [docs].[Files] (
    FileId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FileName NVARCHAR(255) NOT NULL,
    FileExtension NVARCHAR(10) NOT NULL,
    FilePath NVARCHAR(1000) NOT NULL,
    FileSize BIGINT NOT NULL,
    MimeType NVARCHAR(100),
    CategoryId INT NOT NULL,
    CurrentVersionId UNIQUEIDENTIFIER NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CheckedOutBy UNIQUEIDENTIFIER NULL,
    CheckedOutDate DATETIME2 NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy UNIQUEIDENTIFIER NOT NULL,
    ModifiedDate DATETIME2 NULL,
    ModifiedBy UNIQUEIDENTIFIER NULL,
    FOREIGN KEY (CategoryId) REFERENCES [docs].[FileCategories](CategoryId),
    FOREIGN KEY (CheckedOutBy) REFERENCES [auth].[Users](UserId),
    FOREIGN KEY (CreatedBy) REFERENCES [auth].[Users](UserId),
    FOREIGN KEY (ModifiedBy) REFERENCES [auth].[Users](UserId)
);
```
**Features**:
- Check-in/check-out for concurrent editing
- Version control integration
- Full file metadata tracking

### **10. FileCategories** 
**Purpose**: Hierarchical folder structure with inheritance
```sql
CREATE TABLE [docs].[FileCategories] (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    ParentCategoryId INT NULL,
    CategoryPath NVARCHAR(1000) NOT NULL, -- Computed path like "/Clinical/Protocols/"
    Level INT NOT NULL DEFAULT 0, -- Depth in hierarchy
    IsActive BIT NOT NULL DEFAULT 1,
    AllowInheritance BIT NOT NULL DEFAULT 1, -- NEW: Enhanced for permissions
    InheritFromParent BIT NOT NULL DEFAULT 1, -- NEW: Enhanced for permissions
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy UNIQUEIDENTIFIER NOT NULL,
    ModifiedDate DATETIME2 NULL,
    ModifiedBy UNIQUEIDENTIFIER NULL,
    FOREIGN KEY (ParentCategoryId) REFERENCES [docs].[FileCategories](CategoryId),
    FOREIGN KEY (CreatedBy) REFERENCES [auth].[Users](UserId),
    FOREIGN KEY (ModifiedBy) REFERENCES [auth].[Users](UserId)
);
```
**Enhanced Features**:
- ✅ **Hierarchical Structure**: Parent-child relationships
- ✅ **Permission Inheritance**: Configurable inheritance rules  
- ✅ **Path Calculation**: Materialized path for queries

### **11. FileVersions**
**Purpose**: Complete version history for all files
```sql
CREATE TABLE [docs].[FileVersions] (
    VersionId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FileId UNIQUEIDENTIFIER NOT NULL,
    VersionNumber INT NOT NULL,
    VersionLabel NVARCHAR(50), -- e.g., "Draft", "Final", "Approved"
    FilePath NVARCHAR(1000) NOT NULL,
    FileSize BIGINT NOT NULL,
    ChecksumMD5 NVARCHAR(32) NOT NULL,
    ChecksumSHA256 NVARCHAR(64) NOT NULL,
    ChangeDescription NVARCHAR(1000),
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy UNIQUEIDENTIFIER NOT NULL,
    FOREIGN KEY (FileId) REFERENCES [docs].[Files](FileId),
    FOREIGN KEY (CreatedBy) REFERENCES [auth].[Users](UserId)
);
```
**Compliance**: Immutable version history with checksums

### **12. FileMetadata**
**Purpose**: Extensible custom properties for files
```sql
CREATE TABLE [docs].[FileMetadata] (
    MetadataId INT IDENTITY(1,1) PRIMARY KEY,
    FileId UNIQUEIDENTIFIER NOT NULL,
    MetadataKey NVARCHAR(100) NOT NULL,
    MetadataValue NVARCHAR(MAX),
    DataType NVARCHAR(50) NOT NULL DEFAULT 'String',
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy UNIQUEIDENTIFIER NOT NULL,
    FOREIGN KEY (FileId) REFERENCES [docs].[Files](FileId),
    FOREIGN KEY (CreatedBy) REFERENCES [auth].[Users](UserId),
    UNIQUE (FileId, MetadataKey)
);
```
**Use Cases**: Study numbers, approval dates, classification tags

### **13. FileAccess**
**Purpose**: Track who accessed which files when
```sql
CREATE TABLE [docs].[FileAccess] (
    AccessId BIGINT IDENTITY(1,1) PRIMARY KEY,
    FileId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    AccessType NVARCHAR(20) NOT NULL, -- View, Download, Edit, Delete
    IpAddress NVARCHAR(45),
    UserAgent NVARCHAR(500),
    AccessDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (FileId) REFERENCES [docs].[Files](FileId),
    FOREIGN KEY (UserId) REFERENCES [auth].[Users](UserId)
);
```
**Compliance**: Required for CFR 21 Part 11 access tracking

### **14. FileLocks**
**Purpose**: Prevent concurrent editing conflicts
```sql
CREATE TABLE [docs].[FileLocks] (
    LockId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FileId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    LockType NVARCHAR(20) NOT NULL DEFAULT 'Exclusive',
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ExpiresAt DATETIME2 NOT NULL,
    FOREIGN KEY (FileId) REFERENCES [docs].[Files](FileId),
    FOREIGN KEY (UserId) REFERENCES [auth].[Users](UserId)
);
```

---

## 🔍 **audit Schema (5 Tables)**

### **15. AuditTrail**
**Purpose**: Immutable system-wide change log (CFR 21 Part 11)
```sql
CREATE TABLE [audit].[AuditTrail] (
    AuditId BIGINT IDENTITY(1,1) PRIMARY KEY,
    TableName NVARCHAR(128) NOT NULL,
    RecordId NVARCHAR(50) NOT NULL,
    EventTypeId INT NOT NULL,
    OldValues NVARCHAR(MAX), -- JSON
    NewValues NVARCHAR(MAX), -- JSON
    UserId UNIQUEIDENTIFIER NOT NULL,
    IpAddress NVARCHAR(45),
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (EventTypeId) REFERENCES [audit].[AuditEventTypes](EventTypeId),
    FOREIGN KEY (UserId) REFERENCES [auth].[Users](UserId)
);
```
**Protection**: SQL triggers prevent UPDATE/DELETE operations

### **16. FileAuditTrail**
**Purpose**: Specialized audit trail for file operations
```sql
CREATE TABLE [audit].[FileAuditTrail] (
    FileAuditId BIGINT IDENTITY(1,1) PRIMARY KEY,
    FileId UNIQUEIDENTIFIER NOT NULL,
    EventTypeId INT NOT NULL,
    VersionId UNIQUEIDENTIFIER NULL,
    Details NVARCHAR(MAX), -- JSON with operation details
    UserId UNIQUEIDENTIFIER NOT NULL,
    IpAddress NVARCHAR(45),
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (FileId) REFERENCES [docs].[Files](FileId),
    FOREIGN KEY (EventTypeId) REFERENCES [audit].[AuditEventTypes](EventTypeId),
    FOREIGN KEY (VersionId) REFERENCES [docs].[FileVersions](VersionId),
    FOREIGN KEY (UserId) REFERENCES [auth].[Users](UserId)
);
```

### **17. AuditEventTypes**
**Purpose**: Predefined categories for audit events
```sql
CREATE TABLE [audit].[AuditEventTypes] (
    EventTypeId INT IDENTITY(1,1) PRIMARY KEY,
    EventName NVARCHAR(100) UNIQUE NOT NULL,
    Description NVARCHAR(500),
    Category NVARCHAR(50), -- User, File, System, Security
    IsActive BIT NOT NULL DEFAULT 1
);
```
**Standard Events**: CREATE, UPDATE, DELETE, LOGIN, LOGOUT, ACCESS, PERMISSION_CHANGE

### **18. AuditTrailArchive**
**Purpose**: Long-term audit storage for compliance
```sql
CREATE TABLE [audit].[AuditTrailArchive] (
    ArchiveId BIGINT IDENTITY(1,1) PRIMARY KEY,
    OriginalAuditId BIGINT NOT NULL,
    TableName NVARCHAR(128) NOT NULL,
    RecordId NVARCHAR(50) NOT NULL,
    EventTypeId INT NOT NULL,
    OldValues NVARCHAR(MAX),
    NewValues NVARCHAR(MAX),
    UserId UNIQUEIDENTIFIER NOT NULL,
    IpAddress NVARCHAR(45),
    Timestamp DATETIME2 NOT NULL,
    ArchivedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (EventTypeId) REFERENCES [audit].[AuditEventTypes](EventTypeId)
);
```
**Purpose**: Meet long-term retention requirements (7+ years)

### **19. AuditSummary**
**Purpose**: Pre-computed audit statistics for reporting
```sql
CREATE TABLE [audit].[AuditSummary] (
    SummaryId INT IDENTITY(1,1) PRIMARY KEY,
    SummaryDate DATE NOT NULL,
    UserId UNIQUEIDENTIFIER,
    EventTypeId INT,
    TableName NVARCHAR(128),
    EventCount INT NOT NULL DEFAULT 0,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES [auth].[Users](UserId),
    FOREIGN KEY (EventTypeId) REFERENCES [audit].[AuditEventTypes](EventTypeId),
    UNIQUE (SummaryDate, UserId, EventTypeId, TableName)
);
```

---

## ⚙️ **config Schema (3 Tables)**

### **20. SystemConfiguration**
**Purpose**: Application-wide settings and parameters
```sql
CREATE TABLE [config].[SystemConfiguration] (
    ConfigId INT IDENTITY(1,1) PRIMARY KEY,
    ConfigKey NVARCHAR(100) UNIQUE NOT NULL,
    ConfigValue NVARCHAR(MAX),
    DataType NVARCHAR(20) NOT NULL DEFAULT 'String',
    Description NVARCHAR(500),
    IsEncrypted BIT NOT NULL DEFAULT 0,
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy UNIQUEIDENTIFIER NOT NULL,
    FOREIGN KEY (ModifiedBy) REFERENCES [auth].[Users](UserId)
);
```
**Examples**: MaxFileSize, SessionTimeout, EmailSettings

### **21. RetentionPolicies**
**Purpose**: Data lifecycle and archival rules
```sql
CREATE TABLE [config].[RetentionPolicies] (
    PolicyId INT IDENTITY(1,1) PRIMARY KEY,
    PolicyName NVARCHAR(100) UNIQUE NOT NULL,
    TableName NVARCHAR(128) NOT NULL,
    RetentionPeriodDays INT NOT NULL,
    ArchiveAfterDays INT NULL,
    DeleteAfterDays INT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy UNIQUEIDENTIFIER NOT NULL,
    ModifiedDate DATETIME2 NULL,
    ModifiedBy UNIQUEIDENTIFIER NULL,
    FOREIGN KEY (CreatedBy) REFERENCES [auth].[Users](UserId),
    FOREIGN KEY (ModifiedBy) REFERENCES [auth].[Users](UserId)
);
```
**Compliance**: Meet pharmaceutical data retention requirements

### **22. ScheduledJobs**
**Purpose**: Automated system maintenance tasks
```sql
CREATE TABLE [config].[ScheduledJobs] (
    JobId INT IDENTITY(1,1) PRIMARY KEY,
    JobName NVARCHAR(100) UNIQUE NOT NULL,
    JobType NVARCHAR(50) NOT NULL, -- Cleanup, Archive, Backup, Report
    Schedule NVARCHAR(100) NOT NULL, -- Cron expression
    IsActive BIT NOT NULL DEFAULT 1,
    LastRunDate DATETIME2 NULL,
    NextRunDate DATETIME2 NULL,
    LastRunStatus NVARCHAR(20) NULL, -- Success, Failed, Running
    Parameters NVARCHAR(MAX), -- JSON
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy UNIQUEIDENTIFIER NOT NULL,
    FOREIGN KEY (CreatedBy) REFERENCES [auth].[Users](UserId)
);
```

---

## 🔗 **Enhanced Folder Permissions (NEW)**

### **CategoryAccess Table**
**Purpose**: Granular folder-level access control with inheritance
```sql
CREATE TABLE [docs].[CategoryAccess] (
    CategoryAccessId INT IDENTITY(1,1) PRIMARY KEY,
    CategoryId INT NOT NULL,
    UserId UNIQUEIDENTIFIER NULL,      -- Individual user access
    RoleId INT NULL,                   -- Role-based access  
    Permissions INT NOT NULL,          -- Bitwise permissions
    InheritToSubfolders BIT NOT NULL DEFAULT 1,
    InheritToFiles BIT NOT NULL DEFAULT 1,
    ExplicitDeny BIT NOT NULL DEFAULT 0, -- Override inheritance
    GrantedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    GrantedBy UNIQUEIDENTIFIER NOT NULL,
    ExpiresAt DATETIME2 NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (CategoryId) REFERENCES [docs].[FileCategories](CategoryId),
    FOREIGN KEY (UserId) REFERENCES [auth].[Users](UserId),
    FOREIGN KEY (RoleId) REFERENCES [auth].[Roles](RoleId),
    FOREIGN KEY (GrantedBy) REFERENCES [auth].[Users](UserId),
    CHECK (UserId IS NOT NULL OR RoleId IS NOT NULL)
);
```

### **Permission Calculation Logic**
**Stored Procedure**: `sp_CalculateEffectivePermissions`
```sql
-- Calculates final permissions for user on specific folder
-- Combines: Role permissions + User permissions + Inheritance
-- Returns: Final bitwise permission value
```

### **Bitwise Permission Values**
```
View = 1          -- Can see folder/file exists
Download = 2      -- Can download files  
Upload = 4        -- Can add new files
Edit = 8          -- Can modify existing files
Delete = 16       -- Can remove files/folders
Manage = 32       -- Can set permissions
Audit = 64        -- Can view audit logs
AdminAccess = 128 -- Full administrative access
```

---

## 🛡️ **Security Features**

### **SQL Triggers for Audit Protection**
```sql
-- Prevents modification of audit tables
CREATE TRIGGER tr_AuditTrail_PreventModification 
ON [audit].[AuditTrail]
FOR UPDATE, DELETE
AS
BEGIN
    ROLLBACK TRANSACTION;
    THROW 50000, 'Audit trail records cannot be modified or deleted', 1;
END;
```

### **Row-Level Security (Future)**
```sql
-- Users can only see their own data unless they have audit permissions
CREATE SECURITY POLICY UserDataAccess ON [auth].[Users]
ADD FILTER PREDICATE security.CanViewUserData(UserId) = 1;
```

---

## 📊 **Indexing Strategy**

### **Performance Indexes**
```sql
-- User authentication lookups
CREATE INDEX IX_Users_Username ON [auth].[Users](Username) WHERE IsActive = 1;
CREATE INDEX IX_Users_Email ON [auth].[Users](Email) WHERE IsActive = 1;

-- Audit queries by date range
CREATE INDEX IX_AuditTrail_Timestamp ON [audit].[AuditTrail](Timestamp DESC);
CREATE INDEX IX_AuditTrail_UserId_Timestamp ON [audit].[AuditTrail](UserId, Timestamp DESC);

-- File queries by category
CREATE INDEX IX_Files_CategoryId_IsActive ON [docs].[Files](CategoryId, IsActive);

-- Session management
CREATE INDEX IX_UserSessions_UserId_IsActive ON [auth].[UserSessions](UserId, IsActive);
```

---

## 🔄 **Data Relationships Summary**

### **Core Relationships**
- **Users** → Central entity referenced by most tables
- **Roles** → Define permissions via RolePermissions
- **FileCategories** → Hierarchical structure with self-reference
- **Files** → Versioned via FileVersions, categorized via FileCategories
- **AuditTrail** → Links to all entities for change tracking

### **Permission Flow**
1. **User** assigned to **Role(s)**
2. **Role** has **RolePermissions**  
3. **User** can have individual **UserPermissions** (override)
4. **CategoryAccess** provides folder-level permissions
5. **Effective Permissions** = Role + User + Category (calculated)

**Status: ✅ SCHEMA COMPLETE - 19 tables deployed and ready**
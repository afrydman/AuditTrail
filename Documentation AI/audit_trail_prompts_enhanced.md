# Audit Trail System — Developer Implementation Guide

## 📋 Technical Context

### Technology Stack
- **Backend**: .NET 8.0 with Razor MVC
- **Database**: SQL Server 2019
- **Cloud Storage**:  AWS S3
- **Authentication**: ASP.NET Core Identity with JWT tokens
- **Logging**: Serilog with structured logging
- **ORM**: Entity Framework Core 8.0

### Architecture Overview
```
├── Web Layer (Razor MVC)
├── API Layer (REST)
├── Business Logic Layer
├── Data Access Layer (Repository Pattern)
├── Database (SQL Server)
└── Cloud Storage (Azure Blob)
```

### Compliance Reference
- [FDA CFR 21 Part 11 Guidelines](https://www.fda.gov/regulatory-information/search-fda-guidance-documents/part-11-electronic-records-electronic-signatures-scope-and-application)
- Focus on Subpart B (Electronic Records) excluding digital signatures

---

## Implementation Modules

## 1. 🔐 User & Role Management

### Priority: P0 (Critical - Must be completed first)
### Dependencies: None
### Estimated Effort: 2 weeks

### Requirements
Design and implement a user management system with **username/password** login and 14 fixed roles:

**Roles List:**
- Site Admin  
- Study Coordinator  
- Study Investigator  
- Unblinded Study Staff  
- Blinded Monitor  
- Unblinded Monitor  
- Sponsor Support  
- Auditor  
- Blinded Archivist  
- Unblinded Archivist  
- Binder Setup Blinded  
- Quality Control  
- System Support  
- System Team Setup  

### Technical Specifications
```csharp
// Password Requirements
PasswordOptions {
    RequiredLength = 12,
    RequireDigit = true,
    RequireUppercase = true,
    RequireLowercase = true,
    RequireNonAlphanumeric = true,

}

// Session Configuration
SessionTimeout = 30 minutes (configurable)
MaxFailedAttempts = 5
LockoutDuration = until admin user restore it
```

### Acceptance Criteria
- [ ] Users can register with email verification
- [ ] Password meets complexity requirements
- [ ] Account locks after 5 failed attempts
- [ ] Session expires after 30 minutes of inactivity
- [ ] All authentication events logged to audit trail
- [ ] Role assignment only by Site Admin
- [ ] Password reset with email verification
- [ ] MFA support (optional for Phase 1)

### Database Schema
```sql
CREATE TABLE Users (
    UserId UNIQUEIDENTIFIER PRIMARY KEY,
    Username NVARCHAR(100) UNIQUE NOT NULL,
    Email NVARCHAR(255) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(500) NOT NULL,
    RoleId INT NOT NULL,
    IsActive BIT DEFAULT 1,
    FailedLoginAttempts INT DEFAULT 0,
    LockoutEnd DATETIME2 NULL,
    CreatedDate DATETIME2 NOT NULL,
    LastLoginDate DATETIME2 NULL,
    FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
)

CREATE TABLE Roles (
    RoleId INT PRIMARY KEY IDENTITY,
    RoleName NVARCHAR(50) UNIQUE NOT NULL,
    Description NVARCHAR(500),
    CreatedDate DATETIME2 NOT NULL
)
```

---

## 2. 📁 File Upload and Versioning

### Priority: P0 (Critical)
### Dependencies: Module 1 (User Management)
### Estimated Effort: 2 weeks

### Requirements
Implement a file upload system in .NET Razor MVC that handles document versioning and concurrent uploads.

### Technical Specifications
```csharp
// File Configuration
public class FileUploadConfig {
    MaxFileSize = 52428800, // 50MB in bytes
    AllowedExtensions = new[] { ".pdf", ".doc", ".docx" },
    StorageContainer = "audit-trail-documents",
    ChunkSize = 4194304 // 4MB chunks for large files
}

// Versioning Strategy
// Path: /StudyID/DocumentType/FileName_v{version}.{extension}
// Example: /STUDY001/Protocols/Protocol_v1.pdf
```

### Acceptance Criteria
- [ ] Upload supports .pdf, .doc, .docx up to 50MB
- [ ] Files stored in Azure Blob Storage with encryption
- [ ] Automatic version incrementing for same file/path
- [ ] Handle concurrent uploads with proper version sequencing
- [ ] Progress bar for large file uploads
- [ ] Virus scanning before storage (using Azure Defender or similar)
- [ ] Generate unique file identifier (GUID) for internal tracking
- [ ] Metadata extraction (author, created date, modified date)

### Database Schema
```sql
CREATE TABLE Files (
    FileId UNIQUEIDENTIFIER PRIMARY KEY,
    FileName NVARCHAR(255) NOT NULL,
    FilePath NVARCHAR(500) NOT NULL,
    Version INT NOT NULL,
    FileSize BIGINT NOT NULL,
    ContentType NVARCHAR(100) NOT NULL,
    BlobUrl NVARCHAR(1000) NOT NULL,
    Checksum NVARCHAR(64) NOT NULL, -- SHA256
    UploadedBy UNIQUEIDENTIFIER NOT NULL,
    UploadedDate DATETIME2 NOT NULL,
    IsDeleted BIT DEFAULT 0,
    DeletedDate DATETIME2 NULL,
    DeletedBy UNIQUEIDENTIFIER NULL,
    FOREIGN KEY (UploadedBy) REFERENCES Users(UserId),
    UNIQUE(FilePath, FileName, Version)
)
```

---

## 3. 🧾 Audit Trail Logging (CFR 21 Part 11 Compliant)

### Priority: P0 (Critical)
### Dependencies: Modules 1 & 2
### Estimated Effort: 1.5 weeks

### Requirements
Build an **immutable audit trail system** that meets CFR 21 Part 11 requirements.

### Technical Specifications
```csharp
public enum AuditEventType {
    // User Events
    UserLogin, UserLogout, UserLoginFailed, UserLocked,
    UserCreated, UserModified, UserDeactivated,
    PasswordChanged, PasswordReset,
    
    // File Events
    FileUploaded, FileViewed, FileDownloaded,
    FileDeleted, FileRenamed, FileVersionCreated,
    MetadataUpdated, AccessDenied,
    
    // System Events
    SystemStartup, SystemShutdown, BackupCreated,
    ConfigurationChanged
}

public class AuditEntry {
    public Guid AuditId { get; set; }
    public AuditEventType EventType { get; set; }
    public DateTime Timestamp { get; set; } // UTC
    public Guid? UserId { get; set; }
    public string Username { get; set; }
    public string RoleName { get; set; }
    public string IPAddress { get; set; }
    public string UserAgent { get; set; }
    public string EntityType { get; set; }
    public string EntityId { get; set; }
    public string OldValue { get; set; } // JSON
    public string NewValue { get; set; } // JSON
    public string AdditionalData { get; set; } // JSON
}
```

### Acceptance Criteria
- [ ] All user actions logged with timestamp (UTC)
- [ ] All file operations logged with before/after values
- [ ] Logs stored in append-only table (no updates/deletes)
- [ ] Database triggers prevent audit table modifications
- [ ] Query interface with filtering by user, date, event type
- [ ] Export audit logs to CSV/PDF for compliance reviews
- [ ] Real-time audit monitoring dashboard
- [ ] Retention period of 1 year minimum (configurable)

### Database Schema
```sql
CREATE TABLE AuditTrail (
    AuditId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    EventType NVARCHAR(50) NOT NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UserId UNIQUEIDENTIFIER NULL,
    Username NVARCHAR(100) NULL,
    RoleName NVARCHAR(50) NULL,
    IPAddress NVARCHAR(45) NULL,
    UserAgent NVARCHAR(500) NULL,
    EntityType NVARCHAR(100) NULL,
    EntityId NVARCHAR(100) NULL,
    OldValue NVARCHAR(MAX) NULL, -- JSON
    NewValue NVARCHAR(MAX) NULL, -- JSON
    AdditionalData NVARCHAR(MAX) NULL -- JSON
)

-- Create trigger to prevent updates/deletes
CREATE TRIGGER PreventAuditModification
ON AuditTrail
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    RAISERROR('Audit trail records cannot be modified or deleted', 16, 1)
    ROLLBACK TRANSACTION
END
```

---

## 4. 🖥️ User Interface (Razor MVC)

### Priority: P1 (High)
### Dependencies: Modules 1, 2, 3
### Estimated Effort: 2 weeks

### Requirements
Design responsive Razor MVC views with Bootstrap 5.

### Technical Specifications
```csharp
// View Models
public class DashboardViewModel {
    public List<RecentFileViewModel> RecentFiles { get; set; }
    public List<RecentActivityViewModel> RecentActivities { get; set; }
    public UserStatistics Statistics { get; set; }
}

public class FileUploadViewModel {
    [Required]
    public IFormFile File { get; set; }
    
    [Required]
    public string DocumentType { get; set; }
    
    public string StudyId { get; set; }
    public string Comments { get; set; }
}
```

### Acceptance Criteria
- [ ] Responsive design (mobile, tablet, desktop)
- [ ] Dashboard shows last 10 actions and recent uploads
- [ ] Drag-and-drop file upload with progress indicator
- [ ] Advanced search with filters (date, user, file type)
- [ ] Pagination for large result sets (20 items per page)
- [ ] Real-time notifications using SignalR
- [ ] Export search results to Excel/CSV
- [ ] Accessibility compliant (WCAG 2.1 Level AA)

### UI Components
```
Views/
├── Shared/
│   ├── _Layout.cshtml
│   ├── _Navigation.cshtml
│   └── _Notifications.cshtml
├── Dashboard/
│   └── Index.cshtml
├── Files/
│   ├── Index.cshtml (Browse)
│   ├── Upload.cshtml
│   ├── Details.cshtml
│   └── Versions.cshtml
├── Audit/
│   ├── Index.cshtml
│   └── Export.cshtml
└── Users/
    ├── Profile.cshtml
    └── Settings.cshtml
```

---

## 5. 🛡️ File Access Control

### Priority: P1 (High)
### Dependencies: Modules 1, 2
### Estimated Effort: 1 week

### Requirements
Implement role-based access control (RBAC) for file operations.

### Technical Specifications
```csharp
// Permission Matrix
public enum FilePermission {
    View = 1,
    Download = 2,
    Upload = 4,
    Delete = 8,
    ModifyMetadata = 16,
    ViewAuditTrail = 32
}

// Role Permissions Configuration
public class RolePermissionConfig {
    { "Site Admin", FilePermission.All },
    { "Auditor", FilePermission.View | FilePermission.Download | FilePermission.ViewAuditTrail },
    { "Study Investigator", FilePermission.View | FilePermission.Download | FilePermission.Upload },
    // ... additional role mappings
}
```

### Acceptance Criteria
- [ ] Role-based permissions enforced at API and UI level
- [ ] Access denied attempts logged to audit trail
- [ ] Configurable permission matrix (admin UI)
- [ ] Bulk permission updates supported
- [ ] Permission inheritance for folder structures
- [ ] Temporary access grants with expiration
- [ ] Permission change history tracked

### Database Schema
```sql
CREATE TABLE RolePermissions (
    RolePermissionId INT PRIMARY KEY IDENTITY,
    RoleId INT NOT NULL,
    ResourceType NVARCHAR(50) NOT NULL, -- 'File', 'Folder', 'System'
    ResourceId NVARCHAR(100) NULL, -- NULL for global permissions
    Permissions INT NOT NULL, -- Bitwise flags
    GrantedBy UNIQUEIDENTIFIER NOT NULL,
    GrantedDate DATETIME2 NOT NULL,
    ExpiresDate DATETIME2 NULL,
    FOREIGN KEY (RoleId) REFERENCES Roles(RoleId),
    FOREIGN KEY (GrantedBy) REFERENCES Users(UserId)
)
```

---

## 6. 🗃️ Database Design (SQL Server)

### Priority: P0 (Critical - Part of initial setup)
### Dependencies: None
### Estimated Effort: 1 week

### Requirements
Design optimized schema with proper indexing and constraints.

### Performance Specifications
```sql
-- Indexing Strategy
CREATE NONCLUSTERED INDEX IX_Files_UploadedDate 
ON Files(UploadedDate DESC) INCLUDE (FileName, FilePath);

CREATE NONCLUSTERED INDEX IX_AuditTrail_Timestamp 
ON AuditTrail(Timestamp DESC) INCLUDE (EventType, UserId);

CREATE NONCLUSTERED INDEX IX_AuditTrail_UserId 
ON AuditTrail(UserId) INCLUDE (Timestamp, EventType);

-- Partitioning for Audit Trail (by month)
CREATE PARTITION FUNCTION AuditTrailPartition (DATETIME2)
AS RANGE RIGHT FOR VALUES (
    '2024-01-01', '2024-02-01', '2024-03-01' -- etc.
);
```

### Acceptance Criteria
- [ ] All tables have primary keys and appropriate indexes
- [ ] Foreign key constraints enforced
- [ ] Database backup strategy implemented
- [ ] Query execution plans optimized (<100ms for common queries)
- [ ] Partition strategy for large tables
- [ ] Archive strategy for old audit records

---

## 7. 🚫 Compliance & Security Safeguards

### Priority: P0 (Critical)
### Dependencies: All modules
### Estimated Effort: 2 weeks

### Requirements
Comprehensive security implementation beyond basic CFR 21 Part 11.

### Technical Specifications
```csharp
// Encryption Configuration
public class SecurityConfig {
    public string EncryptionAlgorithm = "AES-256-GCM";
    public bool EnableTLS = true;
    public string MinTLSVersion = "1.2";
    public bool EnableHSTS = true;
    public int HSTSMaxAge = 31536000; // 1 year
    public bool EnableCSP = true;
}

// Data Protection
services.AddDataProtection()
    .PersistKeysToAzureBlobStorage(container, "keys.xml")
    .ProtectKeysWithAzureKeyVault(keyIdentifier);
```

### Acceptance Criteria
- [ ] All data encrypted at rest (TDE for SQL, encryption for blobs)
- [ ] All data encrypted in transit (TLS 1.2+)
- [ ] Implement Content Security Policy (CSP)
- [ ] SQL injection prevention (parameterized queries)
- [ ] XSS protection (input validation, output encoding)
- [ ] CSRF tokens for all state-changing operations
- [ ] Rate limiting (100 requests/minute per user)
- [ ] Security headers (HSTS, X-Frame-Options, etc.)
- [ ] Regular security scanning (OWASP ZAP integration)
- [ ] Penetration testing before production

### Compliance Checklist
```
CFR 21 Part 11 Requirements:
□ 11.10(a) - Validation of systems
□ 11.10(b) - Accurate and complete copies
□ 11.10(c) - Record protection
□ 11.10(d) - Limiting system access
□ 11.10(e) - Audit trails
□ 11.10(f) - Operational checks
□ 11.10(g) - Authority checks
□ 11.10(h) - Device checks
□ 11.10(k) - Document controls
□ 11.300(b) - User identification
□ 11.300(d) - Transaction recording
```

---

## 8. 🔌 REST API Layer

### Priority: P2 (Medium)
### Dependencies: Modules 1-7
### Estimated Effort: 2 weeks

### Requirements
RESTful API for external integrations and programmatic access.

### API Endpoints
```yaml
Authentication:
  POST   /api/auth/login
  POST   /api/auth/refresh
  POST   /api/auth/logout

Files:
  GET    /api/files                    # List files
  GET    /api/files/{id}              # Get file details
  GET    /api/files/{id}/download     # Download file
  POST   /api/files/upload            # Upload file
  PUT    /api/files/{id}/metadata     # Update metadata
  DELETE /api/files/{id}              # Soft delete
  GET    /api/files/{id}/versions     # List versions

Audit:
  GET    /api/audit                   # Query audit logs
  GET    /api/audit/export            # Export audit logs
  GET    /api/audit/file/{fileId}     # Audit for specific file
  GET    /api/audit/user/{userId}     # Audit for specific user

Users:
  GET    /api/users                   # List users (admin only)
  GET    /api/users/{id}              # Get user details
  POST   /api/users                   # Create user
  PUT    /api/users/{id}              # Update user
  DELETE /api/users/{id}              # Deactivate user
```

### Technical Specifications
```csharp
// API Configuration
public class ApiConfig {
    public string Version = "v1";
    public int RateLimitPerMinute = 100;
    public int MaxPageSize = 100;
    public string[] AllowedOrigins = { "https://app.domain.com" };
}

// Response Format
public class ApiResponse<T> {
    public bool Success { get; set; }
    public T Data { get; set; }
    public List<string> Errors { get; set; }
    public PaginationMeta Pagination { get; set; }
}

// Authentication: JWT Bearer
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero
        };
    });
```

### Acceptance Criteria
- [ ] OpenAPI/Swagger documentation
- [ ] JWT authentication with refresh tokens
- [ ] Rate limiting per endpoint
- [ ] Request/response logging
- [ ] API versioning support
- [ ] CORS configuration
- [ ] Health check endpoints
- [ ] Integration tests for all endpoints
- [ ] Postman collection for testing

---

## 9. 📊 Data Retention & Archival

### Priority: P2 (Medium)
### Dependencies: Module 3 (Audit Trail)
### Estimated Effort: 1 week

### Requirements
Implement data lifecycle management for compliance and performance.

### Technical Specifications
```csharp
public class RetentionPolicy {
    public int AuditTrailRetentionDays = 365; // Minimum
    public int FileRetentionYears = 7;        // Configurable
    public int ArchiveAfterDays = 180;        // Move to cold storage
    public string ArchiveStorageTier = "Cool"; // Azure Cool tier
}

// Archival Job (runs daily)
public class ArchivalService {
    public async Task ArchiveOldRecords() {
        // 1. Move old audit records to archive table
        // 2. Compress and move old files to cool storage
        // 3. Update metadata to reflect archive status
        // 4. Generate archive report
    }
}
```

### Acceptance Criteria
- [ ] Automated archival job (daily)
- [ ] Archived data remains searchable
- [ ] Restore archived data within 24 hours
- [ ] Archive integrity verification
- [ ] Retention policy configuration UI
- [ ] Legal hold capability
- [ ] Purge process for expired data
- [ ] Archive audit trail maintained

### Database Schema
```sql
CREATE TABLE AuditTrailArchive (
    -- Same structure as AuditTrail
    -- Compressed using SQL Server compression
) WITH (DATA_COMPRESSION = PAGE);

CREATE TABLE RetentionPolicies (
    PolicyId INT PRIMARY KEY IDENTITY,
    EntityType NVARCHAR(50) NOT NULL,
    RetentionDays INT NOT NULL,
    ArchiveAfterDays INT NULL,
    LegalHold BIT DEFAULT 0,
    CreatedBy UNIQUEIDENTIFIER NOT NULL,
    CreatedDate DATETIME2 NOT NULL,
    ModifiedBy UNIQUEIDENTIFIER NULL,
    ModifiedDate DATETIME2 NULL
)
```

---

## 🚀 Development Guidelines

### Code Standards
```csharp
// Naming Conventions
// - PascalCase for public members
// - camelCase for private fields
// - UPPER_CASE for constants
// - Async methods end with "Async"

// Example Service Pattern
public interface IFileService {
    Task<FileDto> UploadFileAsync(FileUploadDto file);
    Task<FileDto> GetFileAsync(Guid fileId);
}

public class FileService : IFileService {
    private readonly IFileRepository _fileRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<FileService> _logger;
    
    // Dependency injection
    public FileService(
        IFileRepository fileRepository,
        IAuditService auditService,
        ILogger<FileService> logger) {
        _fileRepository = fileRepository;
        _auditService = auditService;
        _logger = logger;
    }
}
```

### Testing Requirements
```csharp
// Unit Test Coverage: Minimum 80%
// Integration Test: All API endpoints
// Load Test: 100 concurrent users

[TestClass]
public class FileServiceTests {
    [TestMethod]
    public async Task UploadFile_ValidFile_ReturnsSuccess() {
        // Arrange
        var mockRepo = new Mock<IFileRepository>();
        var service = new FileService(mockRepo.Object);
        
        // Act
        var result = await service.UploadFileAsync(testFile);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedId, result.FileId);
    }
}
```
```

### Performance Benchmarks
- Page load time: < 2 seconds
- File upload (50MB): < 30 seconds
- Audit query (1M records): < 5 seconds
- API response time: < 200ms (95th percentile)
- Database query time: < 100ms
- Concurrent users: 100 minimum

### Monitoring & Logging
```csharp
// Structured logging with Serilog
Log.Information("File uploaded successfully", new {
    FileId = file.Id,
    FileName = file.Name,
    Size = file.Size,
    UserId = currentUser.Id,
    Duration = stopwatch.ElapsedMilliseconds
});

// Application Insights integration
services.AddApplicationInsightsTelemetry();

// Health checks
services.AddHealthChecks()
    .AddSqlServer(connectionString)
    .AddAzureBlobStorage(storageConnection);
```

### Documentation Requirements
- API documentation (Swagger/OpenAPI)
- Database schema documentation
- Deployment guide
- User manual
- Administrator guide
- Troubleshooting guide
- Runbook for common issues

### Security Review Checklist
- [ ] OWASP Top 10 vulnerabilities addressed
- [ ] Penetration testing completed
- [ ] Security headers implemented
- [ ] Sensitive data encrypted
- [ ] Access controls verified
- [ ] Audit logging comprehensive
- [ ] Input validation implemented
- [ ] Output encoding applied
- [ ] Session management secure
- [ ] Error handling doesn't leak information

---

## 📅 Suggested Implementation Timeline

### Phase 1: Foundation (Weeks 1-4)
- Module 1: User & Role Management
- Module 6: Database Design
- Module 2: File Upload (Basic)

### Phase 2: Compliance (Weeks 5-7)
- Module 3: Audit Trail
- Module 7: Security Safeguards
- Module 5: Access Control

### Phase 3: User Experience (Weeks 8-10)
- Module 4: User Interface
- Module 2: File Upload (Advanced features)
- Testing & Bug Fixes

### Phase 4: Integration (Weeks 11-12)
- Module 8: REST API
- Module 9: Data Retention
- Performance Optimization
- Documentation

### Phase 5: Deployment (Weeks 13-14)
- Security Audit
- Load Testing
- User Acceptance Testing
- Production Deployment
- Training & Handover

---

## 🔗 Resources & References

### Technical Documentation
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [Azure Blob Storage](https://docs.microsoft.com/azure/storage/blobs)
- [SQL Server Best Practices](https://docs.microsoft.com/sql/relational-databases/best-practices)

### Compliance Resources
- [FDA 21 CFR Part 11 Guidelines](https://www.fda.gov/regulatory-information)
- [GAMP 5 Guidelines](https://ispe.org/publications/guidance-documents/gamp-5)
- [ISO 27001 Standards](https://www.iso.org/isoiec-27001-information-security.html)

### Security Resources
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [NIST Cybersecurity Framework](https://www.nist.gov/cyberframework)
- [CIS Controls](https://www.cisecurity.org/controls)

---

## 📝 Notes for AI/Developer Sessions

When implementing this system:

1. **Start with the database schema** - Get the foundation right
2. **Implement authentication first** - Everything depends on it
3. **Build audit logging early** - Retrofit is harder
4. **Test security continuously** - Not just at the end
5. **Document as you go** - Don't leave it for later
6. **Use dependency injection** - Makes testing easier
7. **Implement logging everywhere** - You'll need it for debugging
8. **Consider scalability early** - Design for growth
9. **Automate testing** - Manual testing won't scale
10. **Review code regularly** - Catch issues early

### Common Pitfalls to Avoid
- Don't store passwords in plain text
- Don't trust client-side validation alone
- Don't forget about SQL injection
- Don't ignore error handling
- Don't skip unit tests
- Don't hardcode configuration values
- Don't forget about timezone handling (use UTC)
- Don't ignore performance from the start
- Don't skip code reviews
- Don't deploy without a rollback plan

### Questions to Ask Before Starting
1. What's the expected user load?
2. What's the data volume expectation?
3. Are there existing systems to integrate with?
4. What's the deployment environment?
5. What's the backup and disaster recovery plan?
6. Who are the stakeholders?
7. What's the budget for third-party services?
8. What's the timeline flexibility?
9. Are there specific compliance auditors to satisfy?
10. What's the long-term maintenance plan?

---

**Document Version**: 2.1  
**Last Updated**: 2025-09-11  
**Next Review**: Quarterly or as needed
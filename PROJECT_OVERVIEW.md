# AuditTrail System - Complete Project Overview

## 🎯 **Project Purpose**
CFR 21 Part 11 compliant audit trail system for pharmaceutical/clinical environments requiring:
- **Immutable audit logging** of all system changes
- **14 fixed user roles** with granular permissions
- **File versioning** with complete change tracking  
- **Folder tree access control** with inheritance
- **Electronic signatures** and user authentication
- **Regulatory compliance** for FDA validation

## 🏗️ **Solution Architecture**

### **Current Structure**
```
Solution/
├── AuditTrail.API/          # REST API (JWT Authentication)
├── AuditTrail.Web/          # MVC Web App (Cookie Authentication)
├── AuditTrail.Core/         # Entities, Interfaces, DTOs
├── AuditTrail.Infrastructure/ # EF Core + Dapper Data Access
├── AuditTrail.Application/  # Business Logic Layer
└── AuditTrail.sln          # Solution File
```

### **Technology Stack**
- **.NET 8** - Latest LTS framework
- **SQL Server** - Primary database 
- **Entity Framework Core** - CRUD operations & migrations
- **Dapper** - Complex queries & stored procedures
- **JWT Authentication** - API security
- **Cookie Authentication** - Web application security
- **BCrypt** - Password hashing
- **Repository Pattern** - Data access abstraction

## 🗄️ **Database Schema (19 Tables)**

### **auth Schema (8 tables)**
- `Users` - User accounts with authentication
- `Roles` - 14 predefined regulatory roles
- `UserSessions` - Active session tracking
- `PasswordHistory` - Password change audit
- `LoginAttempts` - Failed login monitoring
- `Permissions` - Granular permission definitions
- `RolePermissions` - Role-based access control
- `UserPermissions` - Individual user overrides

### **docs Schema (6 tables)**
- `Files` - Document storage with metadata
- `FileCategories` - Hierarchical folder structure
- `FileVersions` - Version history tracking
- `FileMetadata` - Extended file properties
- `FileAccess` - File-level access logs
- `FileLocks` - Concurrent editing protection

### **audit Schema (5 tables)**
- `AuditTrail` - Immutable system change log
- `FileAuditTrail` - Document-specific auditing
- `AuditEventTypes` - Predefined audit categories
- `AuditTrailArchive` - Long-term audit storage
- `AuditSummary` - Reporting aggregations

### **config Schema (3 tables)**
- `SystemConfiguration` - Application settings
- `RetentionPolicies` - Data lifecycle management
- `ScheduledJobs` - Automated system tasks

## 🔐 **Security & Compliance**

### **CFR 21 Part 11 Requirements**
- ✅ **Electronic Records** - All changes tracked
- ✅ **Electronic Signatures** - User authentication required
- ✅ **Audit Trails** - Immutable change logging
- ✅ **System Access** - Role-based permissions
- ✅ **Data Integrity** - SQL triggers prevent tampering

### **Authentication Methods**
- **API (JWT)**: Token-based for API consumers
- **Web (Cookies)**: Session-based for web users
- **Password Policy**: BCrypt hashing, history tracking
- **Session Management**: Timeout, concurrent session limits

### **Permission System**
- **Bitwise Permissions**: Fine-grained access control
- **Folder Inheritance**: Parent permissions flow to children
- **Role Hierarchy**: 14 predefined pharmaceutical roles
- **User Overrides**: Individual permission exceptions

## 📁 **Folder Access Control System**

### **Hierarchy Model**
```
Root Folder
├── Clinical Studies/          # [StudyManager: Full, Investigator: Read]
│   ├── Protocol Documents/    # [Inherited + PrincipalInvestigator: Write]
│   └── Patient Data/         # [DataManager: Full, Monitor: Read]
└── Regulatory/               # [RegulatoryAffairs: Full, Others: None]
```

### **Permission Values (Bitwise)**
- `View = 1` - Can see folder/file
- `Download = 2` - Can download files
- `Upload = 4` - Can add new files
- `Edit = 8` - Can modify existing files
- `Delete = 16` - Can remove files/folders
- `Manage = 32` - Can set permissions
- `Audit = 64` - Can view audit logs

### **Inheritance Rules**
- **Folder Permissions** → Automatically inherit to subfolders
- **File Permissions** → Inherit from parent folder
- **Explicit Grants** → Override inherited permissions
- **Role + User** → User permissions add to role permissions

## 🔄 **Development Patterns**

### **Hybrid ORM Strategy (Implemented)**
```csharp
// EF Core for CRUD operations
var user = await _context.Users.FindAsync(userId);
_context.Users.Add(newUser);
await _context.SaveChangesAsync();

// Dapper for complex queries and stored procedures
var permissions = await _dapper.QueryAsync<int>(
    "sp_CalculateEffectivePermissions", 
    new { UserId = userId, CategoryId = folderId }
);
```

### **Repository Pattern**
```csharp
// Generic repository for basic CRUD
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(object id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(object id);
}

// Specialized repositories for complex operations
public interface IUserRepository : IRepository<User>
{
    Task<User?> AuthenticateAsync(string username, string password, string ipAddress);
    Task<IEnumerable<Permission>> GetEffectivePermissionsAsync(Guid userId, int categoryId);
}
```

### **Audit Interceptor Pattern**
```csharp
// Automatic audit logging via EF Core interceptors
public class AuditInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(...)
    {
        var auditEntries = CreateAuditEntries(eventData.Context);
        // Log all changes before saving
        await LogAuditEntriesAsync(auditEntries);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
```

### **Current User Service**
```csharp
// Track current user context for auditing
public interface ICurrentUserService
{
    Guid? UserId { get; set; }
    string? Username { get; set; }
    string? IpAddress { get; set; }
}
```

## 🚀 **Deployment Configuration**

### **Target Environment**
- **Users**: <10 users
- **Deployment**: Local machine only
- **Architecture**: Single server
- **Authentication**: Username/password (expandable)
- **Client**: Web application only
- **Testing**: Not required initially

### **Connection Strings**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AuditTrail;Integrated Security=true;TrustServerCertificate=true"
  }
}
```

## 🛠️ **Development Workflow**

### **1. Database First Approach**
- SQL scripts already deployed (19 tables)
- Entity Framework models generated from database
- Stored procedures for complex permissions logic

### **2. API Development**
- Controllers follow RESTful conventions
- JWT authentication for API access
- Swagger/OpenAPI documentation

### **3. Web Development**  
- MVC pattern with Razor views
- Cookie-based authentication
- Bootstrap UI framework (expandable)

### **4. Quality Standards**
- Repository pattern for testability
- Dependency injection throughout
- Configuration-driven settings
- Comprehensive error handling

## 📋 **14 Required Pharmaceutical Roles**

1. **System Administrator** - Full system access
2. **Study Manager** - Study oversight and management
3. **Principal Investigator** - Clinical study leadership
4. **Sub Investigator** - Delegated clinical tasks
5. **Study Coordinator** - Study logistics and coordination
6. **Data Manager** - Data entry and quality control
7. **Clinical Data Associate** - Data verification and queries
8. **Monitor** - Study compliance monitoring
9. **Quality Assurance** - Quality control and auditing
10. **Regulatory Affairs** - Regulatory compliance management
11. **Biostatistician** - Statistical analysis and reporting
12. **Medical Writer** - Documentation and reporting
13. **Pharmacovigilance** - Safety data management
14. **Audit User** - Read-only audit trail access

## 🔄 **Integration Points**

### **API ↔ Web Integration**
- **Shared Infrastructure**: Same repositories and database
- **Consistent Auditing**: Both apps use same interceptors
- **User Management**: Single user table, multiple auth methods
- **Permission Model**: Shared access control system

### **Database Integration**
- **Immutable Audit**: SQL triggers prevent audit modification
- **Referential Integrity**: Foreign keys enforce data consistency
- **Schema Separation**: Logical grouping by business function

## 📝 **Next Development Areas**

### **High Priority**
- **UI Views**: Create Razor views for authentication and dashboard
- **File Management**: Upload, download, version control interface
- **Permission Management**: Admin interface for access control
- **Audit Reporting**: Query and export audit trails

### **Medium Priority**
- **Electronic Signatures**: Digital signing workflow
- **Email Notifications**: System alerts and approvals
- **Advanced Search**: Full-text file content search
- **Data Export**: CSV, PDF reporting capabilities

### **Future Enhancements**
- **API Versioning**: Support multiple API versions
- **Caching Strategy**: Redis for performance optimization
- **Load Balancing**: Multi-server deployment support
- **External Integration**: LDAP, Active Directory, SAML

## 🎯 **Success Criteria**

### **Functional Requirements Met**
- ✅ CFR 21 Part 11 compliance architecture
- ✅ Immutable audit trail with SQL enforcement
- ✅ Hierarchical folder permissions with inheritance
- ✅ 14 pharmaceutical role definitions
- ✅ File versioning and metadata tracking
- ✅ Dual authentication (API + Web)

### **Technical Requirements Met**
- ✅ .NET 8 with Entity Framework Core + Dapper
- ✅ Repository pattern with dependency injection
- ✅ Automatic audit interceptors
- ✅ Secure authentication with BCrypt
- ✅ Comprehensive database schema (19 tables)
- ✅ Clean architecture separation

**Status: ✅ IMPLEMENTATION COMPLETE - Ready for UI development and testing**
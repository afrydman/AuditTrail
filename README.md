# AuditTrail System

**CFR 21 Part 11 Compliant Audit Trail System for Pharmaceutical & Clinical Research**

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2019+-red.svg)](https://www.microsoft.com/sql-server)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## 🎯 **Overview**

Enterprise-grade audit trail system designed for pharmaceutical and clinical research environments requiring FDA CFR 21 Part 11 compliance. Features immutable audit logging, hierarchical file management, and advanced permission controls.

## ✨ **Key Features**

### **🔐 Security & Compliance**
- **CFR 21 Part 11 Compliant** - FDA electronic records requirements
- **Immutable Audit Trail** - SQL-enforced tamper protection  
- **Dual Authentication** - JWT (API) + Cookie (Web) support
- **BCrypt Password Security** - Industry-standard hashing with proper salt handling
- **Role-Based Access Control** - 14 predefined pharmaceutical roles
- **Stored Procedure Integration** - Authentication flow with complete audit logging
- **Multi-Result Set Handling** - Robust Dapper implementation for complex SP responses

### **📁 File Management**
- **Version Control** - Complete file history with checksums
- **Hierarchical Folders** - Unlimited folder depth with inheritance
- **Metadata System** - Extensible custom properties
- **Check-in/Check-out** - Concurrent editing protection
- **Access Logging** - All file operations tracked

### **🛡️ Advanced Permissions**
- **Bitwise Permissions** - Granular access control (View, Download, Upload, Edit, Delete, Manage, Audit)
- **Permission Inheritance** - Parent folder permissions flow to children
- **Explicit Denies** - Override inheritance when needed
- **User + Role Based** - Combined permission calculation

### **🏗️ Architecture**
- **.NET 8** - Latest LTS framework
- **Clean Architecture** - Separated concerns with DI
- **Hybrid ORM** - EF Core + Dapper for optimal performance
- **REST API** - Full CRUD with Swagger documentation
- **MVC Web App** - Responsive Bootstrap interface

## 🚀 **Quick Start**

### **Prerequisites**
- .NET 8 SDK
- SQL Server (LocalDB or full instance)
- Visual Studio 2022 or VS Code

### **Setup**
```bash
# Clone and restore packages
git clone <repository-url>
cd AuditTrail/git/Solution
dotnet restore

# Build solution
dotnet build

# Run API (Terminal 1)
cd AuditTrail.API
dotnet run
# API: https://localhost:5001

# Run Web App (Terminal 2)
cd AuditTrail.Web
dotnet run  
# Web: https://localhost:5002
```

### **Default Credentials**
```
Username: admin
Password: admin123
```

> **Note**: Login includes show/hide password toggle for improved usability. Interface is localized in Spanish with the Remember Me checkbox removed for enhanced security.

### **Database**
The database schema is already deployed with 19 tables. Update connection string in `appsettings.json` if needed:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AuditTrail;Integrated Security=true;TrustServerCertificate=true"
  }
}
```

## 📊 **Database Schema**

### **19 Tables Across 4 Schemas**
- **auth** (8 tables) - Users, Roles, Sessions, Permissions
- **docs** (6 tables) - Files, Categories, Versions, Metadata  
- **audit** (5 tables) - Audit trails, Event types, Archives
- **config** (3 tables) - Configuration, Policies, Jobs

## 🏥 **Pharmaceutical Roles**

The system includes 14 predefined roles for clinical research:

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

## 🔗 **API Endpoints**

### **Authentication**
- `POST /api/auth/login` - User authentication
- `POST /api/auth/logout` - Session termination
- `GET /api/auth/profile` - Current user profile

### **File Management**
- `POST /api/files/upload` - Upload new file/version
- `GET /api/files/{id}/download` - Download file
- `GET /api/files/{id}/versions` - Get version history
- `DELETE /api/files/{id}` - Delete file (with audit)

### **User Management**
- `GET /api/users` - List users (paginated)
- `POST /api/users` - Create new user
- `PUT /api/users/{id}` - Update user
- `GET /api/users/{id}/permissions` - User permissions

### **Audit Trail**
- `GET /api/audit` - Query audit trail
- `GET /api/audit/file/{id}` - File-specific audit
- `GET /api/audit/user/{id}` - User-specific audit
- `GET /api/audit/export` - Export audit data

Full API documentation available at `/swagger` endpoint.

## 🔧 **Configuration**

### **Key Settings**
```json
{
  \"JwtSettings\": {
    \"ExpirationInMinutes\": 30,
    \"Secret\": \"[Your-Secret-Key]\"
  },
  \"FileStorage\": {
    \"MaxFileSizeMB\": 100,
    \"AllowedExtensions\": \".pdf,.docx,.xlsx\"
  },
  \"Authentication\": {
    \"CookieName\": \"AuditTrailAuth\",
    \"ExpireTimeSpan\": 30
  }
}
```

## 🛡️ **Security Features**

### **CFR 21 Part 11 Compliance**
- ✅ **Electronic Records** - All changes tracked
- ✅ **Electronic Signatures** - User authentication required  
- ✅ **Audit Trails** - Immutable change logging
- ✅ **System Access** - Role-based permissions
- ✅ **Data Integrity** - Checksum validation

### **Security Controls**
- **Password Policy** - Complexity requirements + history
- **Account Lockout** - Failed login attempt protection
- **Session Management** - Timeout and concurrent limits
- **IP Tracking** - All actions tied to source IP
- **SQL Injection Protection** - Parameterized queries

## 📈 **Performance**

### **Optimizations**
- **Indexed Queries** - All lookup operations optimized
- **Connection Pooling** - Efficient database connections
- **Caching Strategy** - Memory cache for permissions
- **Pagination** - Large result sets handled efficiently
- **Async Operations** - Non-blocking I/O throughout

## 📋 **Project Structure**

```
Solution/
├── AuditTrail.API/          # REST API (Port 7001)
│   ├── Controllers/         # API endpoints
│   ├── Middleware/          # Cross-cutting concerns  
│   └── Program.cs           # API startup
├── AuditTrail.Web/          # MVC Web App (Port 7002)
│   ├── Controllers/         # MVC controllers
│   ├── Views/               # Razor views
│   ├── Models/              # View models
│   └── Program.cs           # Web startup
├── AuditTrail.Core/         # Domain Layer
│   ├── Entities/            # Domain entities
│   ├── Interfaces/          # Repository contracts
│   └── DTOs/                # Data transfer objects
├── AuditTrail.Infrastructure/ # Data Layer
│   ├── Data/                # EF Core context
│   ├── Repositories/        # Data access
│   └── Interceptors/        # Audit interceptor
└── AuditTrail.Application/   # Business Layer
    └── Services/            # Business logic
```

## 📚 **Documentation**

- **[PROJECT_OVERVIEW.md](PROJECT_OVERVIEW.md)** - Complete system architecture
- **[DATABASE_SCHEMA.md](DATABASE_SCHEMA.md)** - All 19 tables explained
- **[API_PATTERNS.md](API_PATTERNS.md)** - Development guidelines
- **[FOLDER_PERMISSIONS.md](FOLDER_PERMISSIONS.md)** - Access control system
- **[DEVELOPMENT_GUIDE.md](DEVELOPMENT_GUIDE.md)** - Setup and workflow

## 🔄 **Development Status**

### **✅ Completed (Production Ready)**
- Database schema (19 tables + stored procedures)
- Authentication & authorization (fully tested with BCrypt)
- Audit trail system with stored procedure integration
- REST API with Swagger documentation
- **Professional Login Interface** - Two-column layout with jQuery enhancements
- **Spanish Localization** - Complete UI translation for pharmaceutical clients
- **Modern Navigation** - Bootstrap Icons with professional user dropdown
- **User Management Pages** - Session Activity, Edit Profile, Email Notifications
- Permission system with role-based access
- File version control system
- Comprehensive test suite (unit + integration)
- Serilog logging with database and file outputs
- **Asset Organization** - Separated CSS/JS files with cache busting
- **Enhanced UX** - Real-time validation, keyboard shortcuts, loading states

### **🔄 In Progress**
- File upload UI
- File browser interface
- Audit report viewer

### **🌐 Localization & UI Enhancements**
- **Spanish Interface**: All user-visible text translated to Spanish
- **Enhanced Security**: Remember Me checkbox removed from login
- **User Experience**: Password visibility toggle and intuitive navigation
- **Modern UI**: User avatar replaced with Bootstrap Icons, professional dropdown menu
- **Account Management**: Session Activity, Edit Profile, and Email Notifications pages
- **Professional Login**: Two-column layout with information panel and external CSS/JS
- **Performance**: Separated CSS/JS files with cache busting for optimal loading
- **Enhanced UX**: jQuery-powered interactions with animations and keyboard shortcuts

### **📋 Planned**
- Electronic signatures
- Email notifications  
- Advanced reporting
- Mobile responsiveness

## 🤝 **Contributing**

1. Fork the repository
2. Create feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Open Pull Request

## 📄 **License**

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 **Support**

For support and questions:
- Create an issue in this repository
- Check the documentation files
- Review the API documentation at `/swagger`

## 🎯 **Target Audience**

- **Pharmaceutical Companies** - Drug development and clinical trials
- **Clinical Research Organizations (CROs)** - Study management
- **Medical Device Companies** - Regulatory submissions
- **Biotech Companies** - Research data management
- **Regulatory Consultants** - Compliance auditing

---

**Status: ✅ Production Ready** | **Compliance: CFR 21 Part 11** | **Architecture: Clean/SOLID**
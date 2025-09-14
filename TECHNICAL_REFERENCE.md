# Technical Reference - Current System State

**Last Updated**: September 13, 2025  
**Status**: Ready for Core Business Features Implementation

---

## 🏗️ **Architecture Overview**

### **Current Stack**
- **.NET 8**: Latest LTS framework
- **SQL Server**: 19 tables with stored procedures
- **EF Core + Dapper**: Hybrid ORM approach
- **JWT + Cookie Auth**: Dual authentication system
- **Bootstrap + jQuery**: Professional UI framework
- **Serilog**: Database + file logging

### **Project Structure**
```
Solution/
├── AuditTrail.API/          # REST API (Port 5001)
├── AuditTrail.Web/          # MVC Web App (Port 5002) 
├── AuditTrail.Core/         # Domain Layer
├── AuditTrail.Infrastructure/ # Data Layer
├── AuditTrail.Application/  # Business Layer
└── AuditTrail.Tests/        # Test Suite
```

---

## 🔐 **Authentication System**

### **Status**: ✅ Fully Operational
- **Credentials**: admin / admin123
- **BCrypt**: Proper password hashing (no salt concatenation)
- **Dapper**: Multi-result set handling with QueryMultipleAsync
- **Stored Procedures**: sp_ProcessAuthenticationResult, sp_LogAuthenticationAttempt
- **Audit Trail**: Complete CFR 21 Part 11 compliance logging

### **Key Files**
```
/Infrastructure/Repositories/UserRepository.cs    # Authentication logic
/API/Controllers/AuthController.cs                # JWT API endpoints  
/Web/Controllers/AccountController.cs             # Cookie-based web auth
/Tests/LogoutTests.cs                            # Comprehensive test suite
```

---

## 🎨 **UI/UX Components**

### **Login System** ✅ Complete
- **File**: `/Views/Account/Login.cshtml`
- **CSS**: `/wwwroot/css/login.css` (140+ lines)
- **JS**: `/wwwroot/js/login.js` (320+ lines jQuery)
- **Features**: Two-column layout, real-time validation, animations, keyboard shortcuts

### **Navigation System** ✅ Complete  
- **File**: `/Views/Shared/_Layout.cshtml`
- **Avatar**: Bootstrap Icons circular user icon
- **Dropdown**: 4 menu options (Session Activity, Edit Profile, Email Notifications, Logout)
- **Localization**: Complete Spanish translation

### **User Management Pages** ✅ Basic Implementation
```
/Views/Account/SessionActivity.cshtml     # Session history (placeholder)
/Views/Account/EditProfile.cshtml         # Profile editing (readonly)
/Views/Account/EmailNotifications.cshtml  # Email preferences (placeholder)
```

### **Asset Management** ✅ Optimized
- **Cache Busting**: `?v=@cacheBuster` on all CSS/JS
- **Separation**: Login-specific and sidebar-specific assets isolated
- **Performance**: External files enable browser caching

### **Sidebar Navigation** ✅ Complete
- **File**: `/Views/Shared/_Layout.cshtml` (integrated)
- **CSS**: `/wwwroot/css/sidebar.css` (369+ lines)
- **JS**: `/wwwroot/js/sidebar.js` (470+ lines jQuery)
- **Features**: Icon-only collapsed state, hover tooltips, smooth animations, keyboard shortcuts (Ctrl+B)

---

## 📊 **Database Schema**

### **Core Tables** (19 total)
```sql
[auth] Schema (8 tables)
├── Users              # User accounts with BCrypt hashes
├── Roles              # 14 pharmaceutical roles
├── UserRoles          # User-role assignments
├── LoginAttempts      # Authentication audit
├── Sessions           # Session management
├── Permissions        # Permission definitions
├── RolePermissions    # Role-permission mappings
└── UserPermissions    # Direct user permissions

[docs] Schema (6 tables)  
├── FileCategories     # Folder hierarchy
├── Files              # Document storage
├── FileVersions       # Version control
├── FileMetadata       # Custom properties
├── CategoryAccess     # Folder permissions
└── FileDownloads      # Access tracking

[audit] Schema (5 tables)
├── AuditTrail         # Main audit log
├── AuditEventTypes    # Event classification
├── AuditArchive       # Historical data
├── ComplianceReports  # CFR 21 reports
└── AuditSettings      # Configuration
```

### **Connection String Pattern**
```json
"DefaultConnection": "Server=.;Database=AuditTrail;Integrated Security=true;TrustServerCertificate=true"
```
*Note: Always use `Server=.` (not localhost) per user preference*

---

## 🧪 **Testing Framework**

### **Current Test Coverage** ✅ Comprehensive
```
/Tests/AuthenticationIntegrationTests.cs  # End-to-end API testing
/Tests/RealDatabaseAuthTests.cs          # Real database authentication  
/Tests/DapperMultipleResultSetsTest.cs   # Multi-result set handling
/Tests/LogoutTests.cs                    # Logout functionality (4 tests)
/Tests/UserRepositoryTests.cs           # Repository unit tests
```

### **Test Commands**
```bash
cd Solution/AuditTrail.Tests
dotnet test --filter "AuthenticationIntegrationTests"  # API tests
dotnet test --filter "LogoutTests"                     # Logout tests
dotnet test                                            # All tests
```

---

## 🌍 **Spanish Localization**

### **Complete Translation** ✅ Done
- **Login Page**: All text, buttons, messages, features
- **Navigation**: Menu items, breadcrumbs, user dropdown
- **User Pages**: Session Activity, Edit Profile, Email Notifications
- **Form Elements**: Labels, placeholders, validation messages

### **Key Translations**
```
English → Spanish
Dashboard → Panel de Control
Document Management → Gestión de Documentos  
User Management → Gestión de Usuarios
Audit Trail → Registro de Auditoría
Sign In → Iniciar Sesión
Sign Out → Cerrar Sesión
Profile → Perfil
Settings → Configuración
```

---

## 📋 **Next Session Implementation Guide**

### **Priority 1: Email Service** 🔴 Critical
- **Interface**: Create IEmailService with SendAsync methods
- **SMTP Config**: Add EmailSettings to appsettings.json
- **Templates**: HTML email templates for registration, alerts, etc.
- **Integration**: Wire into authentication and audit events

### **Priority 2: SignalR Notifications** 🔴 Critical  
- **Hub**: Create NotificationHub for real-time alerts
- **Client**: Add SignalR JavaScript client to layout
- **UI**: Notification bell icon with count badge
- **Events**: User login, file upload, security alerts

### **Priority 3: Dashboard API** 🟡 High
- **Controller**: DashboardController with metrics endpoints
- **DTOs**: DashboardOverviewDto, UserActivityDto, AuditSummaryDto
- **Queries**: Aggregate data from audit and user tables
- **Widgets**: System overview, user activity, compliance metrics

### **Priority 4: File Management** 🟡 High
- **Upload**: File upload with validation and checksum
- **Storage**: File system integration with metadata
- **Browser**: Hierarchical folder navigation UI
- **Properties**: Custom metadata management system

---

## 🔧 **Development Commands**

### **Build & Run**
```bash
# Build entire solution
cd Solution && dotnet build

# Run API (Terminal 1)
cd AuditTrail.API && dotnet run    # https://localhost:5001

# Run Web (Terminal 2)  
cd AuditTrail.Web && dotnet run    # https://localhost:5002
```

### **Database**
```bash
# Connection test
sqlcmd -S . -d AuditTrail -E -Q "SELECT @@VERSION"

# Verify admin user
sqlcmd -S . -d AuditTrail -E -Q "SELECT Username, IsActive FROM [auth].[Users] WHERE Username = 'admin'"
```

### **Testing**
```bash
# Authentication tests
dotnet test --filter "Authentication"

# All tests
dotnet test --logger "console;verbosity=normal"
```

---

## 📁 **Asset Locations**

### **Stylesheets**
```
/wwwroot/css/main.min.css      # Bootstrap + custom styles
/wwwroot/css/login.css         # Login page specific styles (140+ lines)
/wwwroot/css/sidebar.css       # Sidebar navigation styles (369+ lines)
/wwwroot/fonts/bootstrap/      # Bootstrap Icons
```

### **JavaScript**
```
/wwwroot/js/jquery.min.js      # jQuery 3.x
/wwwroot/js/bootstrap.bundle.min.js  # Bootstrap 5.x
/wwwroot/js/login.js           # Login page jQuery module (320+ lines)
/wwwroot/js/sidebar.js         # Sidebar navigation jQuery module (470+ lines)
/wwwroot/js/custom.js          # Global custom scripts
```

### **Views**
```
/Views/Account/Login.cshtml           # Two-column login layout
/Views/Shared/_Layout.cshtml          # Main application layout
/Views/Account/SessionActivity.cshtml # Session activity page
/Views/Account/EditProfile.cshtml     # Profile editing page
/Views/Account/EmailNotifications.cshtml # Email settings page
```

---

## 🎯 **Success Metrics**

### **Current Status**
- ✅ **Build**: Successful (warnings only, no errors)
- ✅ **Tests**: 100% passing (authentication, logout, integration)
- ✅ **UI**: Professional enterprise-grade design
- ✅ **UX**: Enhanced with jQuery animations and shortcuts
- ✅ **Localization**: Complete Spanish translation
- ✅ **Performance**: Optimized asset loading with cache busting

### **Ready for Next Phase**
The system now has a solid foundation with professional UI/UX and is positioned to implement the core business features (notifications, dashboard, file management) that will transform it from an authentication system into a complete CFR 21 Part 11 compliant audit trail solution for pharmaceutical clients.

---

**Next Session Goal**: Transform the professional UI foundation into a complete business application with notifications, dashboard, and file management capabilities.
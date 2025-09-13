# Development Guide - AuditTrail System

## 🎯 **Quick Start**

### **Prerequisites**
- ✅ **.NET 8 SDK** - Latest LTS version
- ✅ **SQL Server** - LocalDB or full instance  
- ✅ **Visual Studio 2022** or **VS Code**
- ✅ **Git** - Version control

### **Project Structure**
```
C:\Work\oncooo\AuditTrail\git\
├── Solution/                    # .NET Solution
│   ├── AuditTrail.API/         # REST API (JWT Auth)
│   ├── AuditTrail.Web/         # MVC Web App (Cookie Auth)
│   ├── AuditTrail.Core/        # Entities & Interfaces
│   ├── AuditTrail.Infrastructure/ # Data Access (EF + Dapper)
│   ├── AuditTrail.Application/ # Business Logic
│   └── AuditTrail.sln          # Solution File
├── sql/                        # Database Scripts
└── docs/                       # Documentation
```

---

## 🚀 **Getting Started**

### **1. Clone and Setup**
```bash
cd C:\Work\oncooo\AuditTrail\git\Solution
dotnet restore
dotnet build
```

### **2. Database Setup**
The database is already deployed with 19 tables. Connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AuditTrail;Integrated Security=true;TrustServerCertificate=true"
  }
}
```

### **3. Run Applications**
```bash
# Terminal 1 - API Server
cd AuditTrail.API
dotnet run
# API runs on: https://localhost:5001

# Terminal 2 - Web Application
cd AuditTrail.Web  
dotnet run
# Web runs on: https://localhost:5002
```

### **4. Login Credentials**
```
Username: admin
Password: admin123
```

> **✅ Authentication Status**: Fully working with BCrypt + stored procedures + comprehensive test coverage

---

## 🏗️ **Architecture Overview**

### **Clean Architecture Layers**
1. **AuditTrail.Core** - Domain entities, interfaces
2. **AuditTrail.Application** - Business logic, services
3. **AuditTrail.Infrastructure** - Data access, external services
4. **AuditTrail.API** - REST API endpoints
5. **AuditTrail.Web** - MVC web application

### **Technology Stack**
- **Framework**: .NET 8
- **ORM**: Entity Framework Core + Dapper (Hybrid)
- **Database**: SQL Server  
- **Authentication**: JWT (API) + Cookies (Web)
- **Password**: BCrypt hashing
- **DI Container**: Built-in .NET DI

---

## 📝 **Development Workflow**

### **1. Adding New Features**

#### **Step 1: Define Entities (Core)**
```csharp
// AuditTrail.Core/Entities/YourEntity.cs
public class YourEntity : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    // Properties...
}
```

#### **Step 2: Create Repository Interface (Core)**
```csharp
// AuditTrail.Core/Interfaces/IYourRepository.cs
public interface IYourRepository : IRepository<YourEntity>
{
    Task<YourEntity?> GetByNameAsync(string name);
    // Custom methods...
}
```

#### **Step 3: Implement Repository (Infrastructure)**
```csharp
// AuditTrail.Infrastructure/Repositories/YourRepository.cs
public class YourRepository : Repository<YourEntity>, IYourRepository
{
    public YourRepository(AuditTrailDbContext context, IDapperContext dapper) 
        : base(context)
    {
        _dapper = dapper;
    }

    public async Task<YourEntity?> GetByNameAsync(string name)
    {
        return await _context.YourEntities
            .FirstOrDefaultAsync(x => x.Name == name);
    }
}
```

#### **Step 4: Create Service (Application)**  
```csharp
// AuditTrail.Application/Services/YourService.cs
public interface IYourService
{
    Task<Result<YourDto>> CreateAsync(CreateYourRequest request);
}

public class YourService : IYourService
{
    private readonly IYourRepository _repository;
    
    public async Task<Result<YourDto>> CreateAsync(CreateYourRequest request)
    {
        // Validation, business logic
        var entity = new YourEntity { Name = request.Name };
        await _repository.AddAsync(entity);
        return Result<YourDto>.Success(new YourDto(entity));
    }
}
```

#### **Step 5: Create Controller (API/Web)**
```csharp
// AuditTrail.API/Controllers/YourController.cs
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class YourController : ControllerBase
{
    private readonly IYourService _service;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateYourRequest request)
    {
        var result = await _service.CreateAsync(request);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }
}
```

#### **Step 6: Register Services (Program.cs)**
```csharp
// Add to AuditTrail.API/Program.cs and AuditTrail.Web/Program.cs
builder.Services.AddScoped<IYourRepository, YourRepository>();
builder.Services.AddScoped<IYourService, YourService>();
```

### **2. Database Changes**

#### **Adding New Tables**
```sql
-- Create new SQL script: 11_New_Feature.sql
CREATE TABLE [schema].[NewTable] (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy UNIQUEIDENTIFIER NOT NULL,
    FOREIGN KEY (CreatedBy) REFERENCES [auth].[Users](UserId)
);
```

#### **Update DbContext**
```csharp
// AuditTrail.Infrastructure/Data/AuditTrailDbContext.cs
public DbSet<NewTable> NewTables { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    modelBuilder.Entity<NewTable>(entity =>
    {
        entity.ToTable("NewTable", "schema");
        entity.HasKey(e => e.Id);
        // Configure entity...
    });
}
```

---

## 🔐 **Security Guidelines**

### **Authentication Implementation**
```csharp
// JWT for API
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    var user = await _userRepository.AuthenticateAsync(
        request.Username, 
        request.Password,
        HttpContext.Connection.RemoteIpAddress?.ToString()
    );
    
    if (user == null)
        return Unauthorized();
        
    var token = _jwtService.GenerateToken(user);
    return Ok(new { Token = token });
}

// Cookie for Web
[HttpPost]
public async Task<IActionResult> Login(LoginViewModel model)
{
    var user = await _userRepository.AuthenticateAsync(
        model.Username, 
        model.Password,
        HttpContext.Connection.RemoteIpAddress?.ToString()
    );
    
    if (user == null)
    {
        ModelState.AddModelError("", "Invalid credentials");
        return View(model);
    }
    
    var claims = new List<Claim>
    {
        new("UserId", user.UserId.ToString()),
        new(ClaimTypes.Name, user.Username)
    };
    
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await HttpContext.SignInAsync(new ClaimsPrincipal(identity));
    
    return RedirectToAction("Index", "Dashboard");
}
```

### **Permission Checking**
```csharp
// Check folder permissions
public async Task<bool> CanUserAccessFolder(Guid userId, int categoryId, string permission)
{
    var permissions = await _dapperContext.QuerySingleAsync<int>(
        "sp_CalculateEffectivePermissions",
        new { UserId = userId, CategoryId = categoryId },
        commandType: CommandType.StoredProcedure
    );
    
    var permissionValue = Enum.Parse<FolderPermissions>(permission);
    return PermissionHelper.HasPermission(permissions, permissionValue);
}

// Use in controllers
[HttpGet("folder/{categoryId}/files")]
public async Task<IActionResult> GetFiles(int categoryId)
{
    if (!await CanUserAccessFolder(CurrentUserId, categoryId, "View"))
        return Forbid();
        
    // Return files...
}
```

---

## 📊 **Data Access Patterns**

### **EF Core for CRUD**
```csharp
// Simple CRUD operations
public async Task<User?> GetUserAsync(Guid userId)
{
    return await _context.Users
        .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
        .FirstOrDefaultAsync(u => u.UserId == userId);
}

public async Task<User> CreateUserAsync(User user)
{
    _context.Users.Add(user);
    await _context.SaveChangesAsync(); // Audit interceptor runs automatically
    return user;
}
```

### **Dapper for Complex Queries**
```csharp
// Complex permission queries
public async Task<IEnumerable<FolderPermissionDto>> GetUserFolderPermissionsAsync(Guid userId)
{
    return await _dapperContext.QueryAsync<FolderPermissionDto>(@"
        SELECT 
            fc.CategoryId,
            fc.CategoryName,
            fc.CategoryPath,
            dbo.fn_CalculateEffectivePermissions(@UserId, fc.CategoryId) AS Permissions
        FROM [docs].[FileCategories] fc
        WHERE fc.IsActive = 1
        ORDER BY fc.CategoryPath
    ", new { UserId = userId });
}

// Stored procedure calls
public async Task<int> GetEffectivePermissionsAsync(Guid userId, int categoryId)
{
    return await _dapperContext.QuerySingleAsync<int>(
        "sp_CalculateEffectivePermissions",
        new { UserId = userId, CategoryId = categoryId },
        commandType: CommandType.StoredProcedure
    );
}
```

---

## 🔍 **Testing Guidelines**

### **Unit Testing Pattern**
```csharp
[TestFixture]
public class UserServiceTests
{
    private Mock<IUserRepository> _mockRepository;
    private Mock<IPasswordHasher> _mockPasswordHasher;
    private UserService _service;
    
    [SetUp]
    public void Setup()
    {
        _mockRepository = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _service = new UserService(_mockRepository.Object, _mockPasswordHasher.Object);
    }
    
    [Test]
    public async Task CreateUserAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Test123!"
        };
        
        _mockPasswordHasher.Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns(("hashedPassword", "salt"));
        
        _mockRepository.Setup(x => x.AddAsync(It.IsAny<User>()))
            .ReturnsAsync(new User { UserId = Guid.NewGuid() });
        
        // Act
        var result = await _service.CreateUserAsync(request);
        
        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
    }
}
```

### **Integration Testing**
```csharp
[TestFixture]
public class UserControllerIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "admin",
            Password = "Admin123!"
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);
        
        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.IsNotNull(result?.Token);
    }
}
```

---

## 🔧 **Configuration Management**

### **appsettings.json Structure**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AuditTrail;Integrated Security=true;TrustServerCertificate=true"
  },
  "JwtSettings": {
    "Secret": "ThisIsAVerySecretKeyForLocalDevelopmentOnly123!",
    "ExpirationInMinutes": 30,
    "Issuer": "AuditTrail.API",
    "Audience": "AuditTrail.Clients"
  },
  "ApiSettings": {
    "BaseUrl": "https://localhost:7001",
    "Timeout": 30
  },
  "Authentication": {
    "CookieName": "AuditTrailAuth",
    "ExpireTimeSpan": 30,
    "SlidingExpiration": true
  },
  "FileStorage": {
    "BasePath": "C:\\AuditTrail\\Files",
    "MaxFileSizeMB": 100,
    "AllowedExtensions": ".pdf,.docx,.xlsx,.jpg,.png"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

### **Environment-Specific Settings**
```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "JwtSettings": {
    "Secret": "DevelopmentSecretKey123!"
  }
}

// appsettings.Production.json  
{
  "ConnectionStrings": {
    "DefaultConnection": "${CONNECTION_STRING}" // From environment variable
  },
  "JwtSettings": {
    "Secret": "${JWT_SECRET}" // From secure store
  }
}
```

---

## 📈 **Performance Optimization**

### **Database Indexing**
```sql
-- Critical indexes for performance
CREATE INDEX IX_Users_Username_Active ON [auth].[Users](Username) WHERE IsActive = 1;
CREATE INDEX IX_Users_Email_Active ON [auth].[Users](Email) WHERE IsActive = 1;
CREATE INDEX IX_AuditTrail_Timestamp ON [audit].[AuditTrail](Timestamp DESC);
CREATE INDEX IX_Files_CategoryId_IsActive ON [docs].[Files](CategoryId, IsActive);
CREATE INDEX IX_CategoryAccess_Lookup ON [docs].[CategoryAccess](CategoryId, UserId, RoleId) WHERE IsActive = 1;
```

### **Caching Strategy**
```csharp
// Memory caching for permissions
public class CachedPermissionService
{
    private readonly IMemoryCache _cache;
    private const int CacheMinutes = 5; // Short cache for security data
    
    public async Task<int> GetUserPermissionsAsync(Guid userId, int categoryId)
    {
        var key = $"permissions_{userId}_{categoryId}";
        
        if (!_cache.TryGetValue(key, out int permissions))
        {
            permissions = await CalculatePermissionsAsync(userId, categoryId);
            _cache.Set(key, permissions, TimeSpan.FromMinutes(CacheMinutes));
        }
        
        return permissions;
    }
}
```

### **Query Optimization**
```csharp
// Efficient pagination
public async Task<PagedResult<FileDto>> GetPagedFilesAsync(int categoryId, int page, int size)
{
    var query = _context.Files
        .Where(f => f.CategoryId == categoryId && f.IsActive)
        .OrderByDescending(f => f.CreatedDate);
    
    var totalCount = await query.CountAsync();
    
    var files = await query
        .Skip((page - 1) * size)
        .Take(size)
        .Select(f => new FileDto
        {
            FileId = f.FileId,
            FileName = f.FileName,
            FileSize = f.FileSize,
            CreatedDate = f.CreatedDate
        })
        .ToListAsync();
    
    return new PagedResult<FileDto>(files, totalCount, page, size);
}
```

---

## 🔄 **Deployment Process**

### **Local Development**
```bash
# Setup database
sqlcmd -S localhost -i sql/01_Create_Database.sql
sqlcmd -S localhost -i sql/02_Create_Tables.sql
# ... run all SQL scripts

# Run applications
dotnet run --project AuditTrail.API --environment Development
dotnet run --project AuditTrail.Web --environment Development
```

### **Production Deployment**
```bash
# Build release
dotnet build --configuration Release

# Publish applications
dotnet publish AuditTrail.API -c Release -o ./publish/api
dotnet publish AuditTrail.Web -c Release -o ./publish/web

# Database migrations
dotnet ef database update --project AuditTrail.Infrastructure --startup-project AuditTrail.API
```

---

## 🐛 **Debugging Guidelines**

### **Common Issues**

#### **Database Connection Issues**
```bash
# Check SQL Server status
services.msc -> SQL Server (MSSQLSERVER)

# Test connection string
sqlcmd -S localhost -E -Q "SELECT @@VERSION"
```

#### **Authentication Issues**
```csharp
// Debug JWT token
[HttpGet("debug/token")]
public IActionResult DebugToken()
{
    var token = HttpContext.Request.Headers["Authorization"]
        .FirstOrDefault()?.Split(" ").Last();
    
    if (string.IsNullOrEmpty(token))
        return BadRequest("No token provided");
    
    var handler = new JwtSecurityTokenHandler();
    var jsonToken = handler.ReadJwtToken(token);
    
    return Ok(new
    {
        Claims = jsonToken.Claims.Select(c => new { c.Type, c.Value }),
        Expires = jsonToken.ValidTo,
        IsExpired = jsonToken.ValidTo < DateTime.UtcNow
    });
}
```

#### **Permission Issues**
```csharp
// Debug user permissions
[HttpGet("debug/permissions/{categoryId}")]
public async Task<IActionResult> DebugPermissions(int categoryId)
{
    var userId = Guid.Parse(HttpContext.User.FindFirst("UserId").Value);
    
    var result = await _dapperContext.QueryMultipleAsync(@"
        -- User roles
        SELECT r.RoleName FROM [auth].[UserRoles] ur
        INNER JOIN [auth].[Roles] r ON ur.RoleId = r.RoleId
        WHERE ur.UserId = @UserId AND ur.IsActive = 1;
        
        -- Direct permissions  
        SELECT * FROM [docs].[CategoryAccess]
        WHERE CategoryId = @CategoryId AND UserId = @UserId AND IsActive = 1;
        
        -- Role permissions
        SELECT ca.* FROM [docs].[CategoryAccess] ca
        INNER JOIN [auth].[UserRoles] ur ON ca.RoleId = ur.RoleId
        WHERE ca.CategoryId = @CategoryId AND ur.UserId = @UserId 
        AND ca.IsActive = 1 AND ur.IsActive = 1;
        
        -- Effective permissions
        EXEC sp_CalculateEffectivePermissions @UserId, @CategoryId;
    ", new { UserId = userId, CategoryId = categoryId });
    
    var roles = await result.ReadAsync<string>();
    var userPermissions = await result.ReadAsync<CategoryAccess>();
    var rolePermissions = await result.ReadAsync<CategoryAccess>();
    var effectivePermissions = await result.ReadSingleAsync<int>();
    
    return Ok(new
    {
        UserId = userId,
        CategoryId = categoryId,
        Roles = roles,
        UserPermissions = userPermissions,
        RolePermissions = rolePermissions,
        EffectivePermissions = effectivePermissions,
        PermissionNames = PermissionHelper.GetPermissionNames(effectivePermissions)
    });
}
```

---

## 📚 **Code Standards**

### **Naming Conventions**
- **Classes**: PascalCase (`UserService`, `FileRepository`)
- **Methods**: PascalCase (`GetUserAsync`, `CreateFileAsync`)  
- **Properties**: PascalCase (`UserId`, `FileName`)
- **Variables**: camelCase (`userId`, `fileName`)
- **Constants**: UPPER_CASE (`MAX_FILE_SIZE`)
- **Database**: snake_case for SQL, PascalCase for C#

### **File Organization**
```
Controllers/
├── AuthController.cs
├── UsersController.cs
└── FilesController.cs

Services/
├── IUserService.cs
├── UserService.cs
├── IFileService.cs
└── FileService.cs

Models/
├── Requests/
│   ├── CreateUserRequest.cs
│   └── LoginRequest.cs
├── Responses/
│   ├── UserDto.cs
│   └── LoginResponse.cs
└── ViewModels/
    └── LoginViewModel.cs
```

### **Error Handling**
```csharp
// Use Result<T> pattern consistently
public async Task<Result<UserDto>> CreateUserAsync(CreateUserRequest request)
{
    try
    {
        // Validation
        if (string.IsNullOrEmpty(request.Username))
            return Result<UserDto>.Failure("Username is required");
        
        // Business logic
        var user = await _repository.AddAsync(newUser);
        return Result<UserDto>.Success(new UserDto(user));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating user: {Username}", request.Username);
        return Result<UserDto>.Failure("An error occurred while creating the user");
    }
}
```

---

## 🚀 **Next Steps**

### **Immediate Development Priorities**
1. **UI Views**: Create Razor views for authentication and file management
2. **File Upload**: Implement file upload with version control
3. **Permission Management**: Admin interface for folder permissions
4. **Audit Reports**: Query and export audit trail data

### **Future Enhancements**
1. **Electronic Signatures**: Digital signing workflow
2. **Email Notifications**: System alerts and approvals
3. **Advanced Search**: Full-text search across files
4. **Mobile Support**: Responsive design improvements
5. **API Versioning**: Support for multiple API versions

### **Production Readiness**
1. **Load Testing**: Performance under concurrent users
2. **Security Audit**: Penetration testing and vulnerability assessment
3. **Backup Strategy**: Database backup and recovery procedures
4. **Monitoring**: Application performance monitoring and alerting

**Status: ✅ DEVELOPMENT GUIDE COMPLETE - Ready for team onboarding**
# API Development Patterns & Guidelines

## 🏗️ **Architecture Pattern: Hybrid ORM (Implemented)**

### **Strategic Decision**
**Option C: EF Core + Dapper Hybrid** - Best of both worlds
- **EF Core**: CRUD operations, migrations, change tracking
- **Dapper**: Complex queries, stored procedures, performance

```csharp
// EF Core for simple CRUD
var user = await _context.Users.FindAsync(userId);
_context.Users.Add(newUser);
await _context.SaveChangesAsync();

// Dapper for complex queries
var permissions = await _dapper.QueryAsync<int>(
    "sp_CalculateEffectivePermissions", 
    new { UserId = userId, CategoryId = folderId },
    commandType: CommandType.StoredProcedure
);
```

---

## 🔧 **Repository Pattern Implementation**

### **Generic Repository Interface**
```csharp
public interface IRepository<T> where T : BaseEntity
{
    // Basic CRUD operations
    Task<T?> GetByIdAsync(object id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(object id);
    Task<bool> ExistsAsync(object id);
    
    // Pagination support
    Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, 
        Expression<Func<T, bool>>? filter = null);
}
```

### **Specialized Repository Pattern**
```csharp
public interface IUserRepository : IRepository<User>
{
    // Authentication methods
    Task<User?> AuthenticateAsync(string username, string password, string ipAddress);
    Task<bool> ValidatePasswordAsync(Guid userId, string password);
    Task UpdatePasswordAsync(Guid userId, string newPasswordHash, string salt);
    
    // Account management
    Task LockUserAccountAsync(Guid userId);
    Task UnlockUserAccountAsync(Guid userId);
    Task IncrementFailedLoginAsync(string username);
    Task ResetFailedLoginAttemptsAsync(Guid userId);
    
    // Permission queries (Dapper)
    Task<IEnumerable<Permission>> GetEffectivePermissionsAsync(Guid userId, int? categoryId = null);
    Task<bool> HasPermissionAsync(Guid userId, string permissionName, int? categoryId = null);
    
    // Session management
    Task<UserSession> CreateSessionAsync(Guid userId, string token, string ipAddress, string userAgent);
    Task InvalidateSessionAsync(string sessionToken);
    Task<IEnumerable<UserSession>> GetActiveSessionsAsync(Guid userId);
}
```

### **Repository Implementation Pattern**
```csharp
public class UserRepository : Repository<User>, IUserRepository
{
    private readonly IDapperContext _dapperContext;
    
    public UserRepository(AuditTrailDbContext context, IDapperContext dapperContext) 
        : base(context)
    {
        _dapperContext = dapperContext;
    }

    // EF Core for simple operations
    public override async Task<User?> GetByIdAsync(object id)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == (Guid)id);
    }

    // Dapper for complex queries
    public async Task<IEnumerable<Permission>> GetEffectivePermissionsAsync(Guid userId, int? categoryId = null)
    {
        using var connection = _dapperContext.CreateConnection();
        return await connection.QueryAsync<Permission>(
            "sp_CalculateEffectivePermissions",
            new { UserId = userId, CategoryId = categoryId },
            commandType: CommandType.StoredProcedure
        );
    }
}
```

---

## 🔒 **Authentication & Authorization Patterns**

### **JWT Authentication (API)**
```csharp
[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtService;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userRepository.AuthenticateAsync(
            request.Username, 
            request.Password, 
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        if (user == null)
            return Unauthorized(new { Message = "Invalid credentials" });

        var token = await _jwtService.GenerateTokenAsync(user);
        
        return Ok(new LoginResponse 
        { 
            Token = token, 
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            User = new UserDto(user)
        });
    }
}
```

### **Cookie Authentication (Web)**
```csharp
[Route("Account")]
public class AccountController : Controller
{
    [HttpPost("Login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userRepository.AuthenticateAsync(
            model.Username, 
            model.Password,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        if (user == null)
        {
            ModelState.AddModelError("", "Invalid username or password");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new("UserId", user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(principal, new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddMinutes(30)
        });

        return RedirectToAction("Index", "Dashboard");
    }
}
```

---

## 📝 **Audit Interceptor Pattern**

### **EF Core Interceptor Implementation**
```csharp
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IAuditRepository _auditRepository;
    private readonly ICurrentUserService _currentUserService;

    public AuditInterceptor(IAuditRepository auditRepository, ICurrentUserService currentUserService)
    {
        _auditRepository = auditRepository;
        _currentUserService = currentUserService;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken = default)
    {
        var auditEntries = CreateAuditEntries(eventData.Context);
        
        // Save changes first
        var saveResult = await base.SavingChangesAsync(eventData, result, cancellationToken);
        
        // Then log audit entries
        if (auditEntries.Any())
        {
            await LogAuditEntriesAsync(auditEntries);
        }
        
        return saveResult;
    }

    private List<AuditEntry> CreateAuditEntries(DbContext? context)
    {
        if (context == null) return new List<AuditEntry>();

        var auditEntries = new List<AuditEntry>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is BaseEntity entity && entry.State != EntityState.Unchanged)
            {
                var auditEntry = new AuditEntry
                {
                    TableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name,
                    RecordId = entity.Id?.ToString() ?? "Unknown",
                    EventType = entry.State.ToString(),
                    UserId = _currentUserService.UserId ?? Guid.Empty,
                    IpAddress = _currentUserService.IpAddress,
                    Timestamp = DateTime.UtcNow,
                    OldValues = entry.State == EntityState.Modified ? SerializeEntity(entry.OriginalValues) : null,
                    NewValues = entry.State != EntityState.Deleted ? SerializeEntity(entry.CurrentValues) : null
                };
                
                auditEntries.Add(auditEntry);
            }
        }

        return auditEntries;
    }
}
```

---

## 🔄 **Result Pattern Implementation**

### **Result<T> Pattern for Error Handling**
```csharp
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? Error { get; private set; }
    public List<string> ValidationErrors { get; private set; } = new();

    public static Result<T> Success(T data) => new() { IsSuccess = true, Data = data };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
    public static Result<T> ValidationFailure(IEnumerable<string> errors) => new() 
    { 
        IsSuccess = false, 
        ValidationErrors = errors.ToList() 
    };
}

// Usage in controllers
[HttpPost]
public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
{
    var result = await _userService.CreateUserAsync(request);
    
    return result.IsSuccess 
        ? Ok(result.Data)
        : BadRequest(new { result.Error, result.ValidationErrors });
}
```

---

## 🏃‍♂️ **Service Layer Pattern**

### **Business Logic Services**
```csharp
public interface IUserService
{
    Task<Result<UserDto>> CreateUserAsync(CreateUserRequest request);
    Task<Result<UserDto>> UpdateUserAsync(Guid userId, UpdateUserRequest request);
    Task<Result<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task<Result<IEnumerable<PermissionDto>>> GetUserPermissionsAsync(Guid userId, int? categoryId = null);
}

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IValidator<CreateUserRequest> _validator;

    public async Task<Result<UserDto>> CreateUserAsync(CreateUserRequest request)
    {
        // 1. Validate request
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return Result<UserDto>.ValidationFailure(validationResult.Errors.Select(e => e.ErrorMessage));

        // 2. Check for duplicates
        var existingUser = await _userRepository.FindAsync(u => 
            u.Username == request.Username || u.Email == request.Email);
        if (existingUser.Any())
            return Result<UserDto>.Failure("Username or email already exists");

        // 3. Create user
        var (hash, salt) = _passwordHasher.HashPassword(request.Password);
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = hash,
            Salt = salt,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedBy = _currentUserService.UserId ?? Guid.Empty
        };

        var createdUser = await _userRepository.AddAsync(user);
        return Result<UserDto>.Success(new UserDto(createdUser));
    }
}
```

---

## 📊 **File Management Patterns**

### **File Upload with Versioning**
```csharp
[HttpPost("upload")]
[RequestSizeLimit(100_000_000)] // 100MB limit
public async Task<IActionResult> UploadFile(
    [FromForm] IFormFile file, 
    [FromForm] int categoryId,
    [FromForm] string? description)
{
    if (file == null || file.Length == 0)
        return BadRequest("No file uploaded");

    // Check permissions
    var hasPermission = await _userRepository.HasPermissionAsync(
        CurrentUserId, "Upload", categoryId);
    if (!hasPermission)
        return Forbid();

    // Create file record
    var fileEntity = new Core.Entities.File
    {
        FileName = file.FileName,
        FileExtension = Path.GetExtension(file.FileName),
        FileSize = file.Length,
        MimeType = file.ContentType,
        CategoryId = categoryId,
        CreatedBy = CurrentUserId
    };

    // Save file to storage
    var filePath = await _fileStorageService.SaveFileAsync(file, fileEntity.FileId);
    fileEntity.FilePath = filePath;

    // Create initial version
    var version = new FileVersion
    {
        FileId = fileEntity.FileId,
        VersionNumber = 1,
        VersionLabel = "Initial",
        FilePath = filePath,
        FileSize = file.Length,
        ChecksumMD5 = await CalculateMD5(file),
        ChecksumSHA256 = await CalculateSHA256(file),
        ChangeDescription = description ?? "Initial upload",
        CreatedBy = CurrentUserId
    };

    await _fileRepository.AddAsync(fileEntity);
    await _fileVersionRepository.AddAsync(version);

    return Ok(new FileDto(fileEntity));
}
```

### **File Download with Access Logging**
```csharp
[HttpGet("{fileId}/download")]
public async Task<IActionResult> DownloadFile(Guid fileId)
{
    var file = await _fileRepository.GetByIdAsync(fileId);
    if (file == null || !file.IsActive)
        return NotFound();

    // Check permissions
    var hasPermission = await _userRepository.HasPermissionAsync(
        CurrentUserId, "Download", file.CategoryId);
    if (!hasPermission)
        return Forbid();

    // Log access
    await _fileAccessRepository.LogAccessAsync(new FileAccess
    {
        FileId = fileId,
        UserId = CurrentUserId,
        AccessType = "Download",
        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
        UserAgent = HttpContext.Request.Headers["User-Agent"]
    });

    var fileBytes = await _fileStorageService.GetFileAsync(file.FilePath);
    return File(fileBytes, file.MimeType ?? "application/octet-stream", file.FileName);
}
```

---

## 🔍 **Permission Checking Patterns**

### **Attribute-Based Authorization**
```csharp
public class RequirePermissionAttribute : AuthorizeAttribute, IAuthorizationFilter
{
    private readonly string _permission;
    private readonly bool _requireCategoryId;

    public RequirePermissionAttribute(string permission, bool requireCategoryId = false)
    {
        _permission = permission;
        _requireCategoryId = requireCategoryId;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var userIdClaim = context.HttpContext.User.FindFirst("UserId")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var userRepository = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
        
        int? categoryId = null;
        if (_requireCategoryId)
        {
            var categoryIdParam = context.RouteData.Values["categoryId"]?.ToString();
            if (int.TryParse(categoryIdParam, out var catId))
                categoryId = catId;
            else
            {
                context.Result = new BadRequestObjectResult("CategoryId required");
                return;
            }
        }

        var hasPermission = userRepository.HasPermissionAsync(userId, _permission, categoryId).Result;
        if (!hasPermission)
        {
            context.Result = new ForbidResult();
        }
    }
}

// Usage
[HttpGet("category/{categoryId}/files")]
[RequirePermission("View", requireCategoryId: true)]
public async Task<IActionResult> GetCategoryFiles(int categoryId)
{
    // Implementation
}
```

---

## 📝 **API Documentation Patterns**

### **Swagger/OpenAPI Configuration**
```csharp
// Program.cs
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AuditTrail API",
        Version = "v1",
        Description = "CFR 21 Part 11 Compliant Audit Trail System",
        Contact = new OpenApiContact
        {
            Name = "AuditTrail Support",
            Email = "support@audittrail.com"
        }
    });

    // JWT Bearer token support
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});
```

### **Controller Documentation Standards**
```csharp
/// <summary>
/// Manages user accounts and authentication
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    /// <summary>
    /// Gets a user by their unique identifier
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <returns>User information if found</returns>
    /// <response code="200">User found and returned successfully</response>
    /// <response code="404">User not found</response>
    /// <response code="403">Access denied - insufficient permissions</response>
    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(UserDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetUser(Guid userId)
    {
        // Implementation
    }
}
```

---

## ⚡ **Performance Patterns**

### **Caching Strategy**
```csharp
public class CachedUserRepository : IUserRepository
{
    private readonly IUserRepository _baseRepository;
    private readonly IMemoryCache _cache;
    private const int CacheMinutes = 15;

    public async Task<User?> GetByIdAsync(object id)
    {
        var cacheKey = $"user_{id}";
        
        if (_cache.TryGetValue(cacheKey, out User? cachedUser))
            return cachedUser;

        var user = await _baseRepository.GetByIdAsync(id);
        if (user != null)
        {
            _cache.Set(cacheKey, user, TimeSpan.FromMinutes(CacheMinutes));
        }

        return user;
    }

    public async Task UpdateAsync(User entity)
    {
        await _baseRepository.UpdateAsync(entity);
        
        // Invalidate cache
        _cache.Remove($"user_{entity.UserId}");
    }
}
```

### **Pagination Pattern**
```csharp
public class PagedResult<T>
{
    public IEnumerable<T> Data { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}

// Usage in repository
public async Task<PagedResult<User>> GetPagedAsync(int pageNumber, int pageSize, 
    Expression<Func<User, bool>>? filter = null)
{
    var query = _context.Users.Where(u => u.IsActive);
    
    if (filter != null)
        query = query.Where(filter);

    var totalCount = await query.CountAsync();
    var data = await query
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return new PagedResult<User>
    {
        Data = data,
        TotalCount = totalCount,
        PageNumber = pageNumber,
        PageSize = pageSize
    };
}
```

---

## 🔒 **Security Patterns**

### **Input Validation**
```csharp
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .Length(3, 50)
            .Matches("^[a-zA-Z0-9_]+$")
            .WithMessage("Username must contain only letters, numbers, and underscores");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]")
            .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character");
    }
}
```

### **Rate Limiting**
```csharp
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    
    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var endpoint = context.Request.Path;
        var key = $"rate_limit_{clientId}_{endpoint}";
        
        var requests = _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return new List<DateTime>();
        });

        requests.Add(DateTime.UtcNow);
        
        // Allow 10 requests per minute per IP per endpoint
        if (requests.Count > 10)
        {
            context.Response.StatusCode = 429; // Too Many Requests
            await context.Response.WriteAsync("Rate limit exceeded");
            return;
        }

        await _next(context);
    }
}
```

---

## 📋 **Error Handling Patterns**

### **Global Exception Handler**
```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var response = exception switch
        {
            ValidationException => new { StatusCode = 400, Message = "Validation failed", Details = exception.Message },
            UnauthorizedAccessException => new { StatusCode = 401, Message = "Unauthorized access" },
            NotFoundException => new { StatusCode = 404, Message = "Resource not found" },
            _ => new { StatusCode = 500, Message = "An error occurred while processing your request" }
        };

        context.Response.StatusCode = response.StatusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

## 🚀 **Development Standards**

### **MUST Follow Patterns**
- ✅ **Repository Pattern**: All data access through repositories
- ✅ **Dependency Injection**: Constructor injection throughout
- ✅ **Result Pattern**: Consistent error handling and responses
- ✅ **Audit Interceptor**: Automatic change tracking
- ✅ **Authentication**: JWT for API, Cookies for Web
- ✅ **Validation**: FluentValidation for all inputs
- ✅ **Documentation**: XML comments for all public APIs

### **Configuration Standards**
- **Connection Strings**: Stored in appsettings.json
- **Secrets**: Use User Secrets in development
- **Environment Variables**: Override settings per environment
- **Dependency Registration**: Scoped for repositories, services

**Status: ✅ PATTERNS DOCUMENTED - Ready for implementation**
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using AuditTrail.Infrastructure.Data;
using AuditTrail.Infrastructure.Repositories;
using AuditTrail.Infrastructure.Interceptors;
using AuditTrail.Core.Interfaces;
using AuditTrail.Web.Middleware;

// Configure Serilog before creating the builder
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Web")
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .WriteTo.File(
        path: $"logs/web-{DateTime.Now:yyyy-MM-dd}.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{Application}] {Message:lj} {Properties:j}{NewLine}{Exception}",
        restrictedToMinimumLevel: LogEventLevel.Debug)
    .WriteTo.MSSqlServer(
        connectionString: "Server=.;Database=AuditTrail;Trusted_Connection=true;TrustServerCertificate=true;",
        sinkOptions: new MSSqlServerSinkOptions
        {
            TableName = "WebLogs",
            SchemaName = "logging",
            AutoCreateSqlTable = false
        },
        restrictedToMinimumLevel: LogEventLevel.Information)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Use Serilog for logging
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "AuditTrailSession";
});

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Configure Entity Framework with SQL Server
builder.Services.AddDbContext<AuditTrailDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    
    // Add audit interceptor
    var auditRepo = serviceProvider.GetService<IAuditRepository>();
    var currentUserService = serviceProvider.GetService<ICurrentUserService>();
    if (auditRepo != null && currentUserService != null)
    {
        options.AddInterceptors(new AuditInterceptor(auditRepo, currentUserService));
    }
});

// Configure Dapper
builder.Services.AddSingleton<IDapperContext, DapperContext>();

// Configure repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();

// Configure current user service
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Configure Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("Authentication:ExpireTimeSpan", 30));
        options.SlidingExpiration = builder.Configuration.GetValue<bool>("Authentication:SlidingExpiration", true);
        options.Cookie.Name = builder.Configuration.GetValue<string>("Authentication:CookieName", "AuditTrailAuth");
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

// Add authorization
builder.Services.AddAuthorization();

// Add HttpClient for API calls
builder.Services.AddHttpClient("AuditTrailApi", client =>
{
    var apiBaseUrl = builder.Configuration.GetValue<string>("ApiSettings:BaseUrl", "https://localhost:7001");
    client.BaseAddress = new Uri(apiBaseUrl ?? "https://localhost:7001");
    client.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("ApiSettings:Timeout", 30));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseSession();

// Add custom middleware after session is configured
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseRouting();

// Add authentication before authorization
app.UseAuthentication();
app.UseAuthorization();

// Middleware to populate CurrentUserService
app.Use(async (context, next) =>
{
    var currentUserService = context.RequestServices.GetService<ICurrentUserService>();
    if (currentUserService != null)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst("UserId")?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                currentUserService.UserId = userId;
                currentUserService.Username = context.User.Identity.Name;
            }
        }
        
        currentUserService.IpAddress = context.Connection.RemoteIpAddress?.ToString();
    }
    
    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

try
{
    Log.Information("Starting AuditTrail Web application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "AuditTrail Web application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
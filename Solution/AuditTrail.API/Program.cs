using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using AuditTrail.Infrastructure.Data;
using AuditTrail.Infrastructure.Repositories;
using AuditTrail.Infrastructure.Interceptors;
using AuditTrail.Core.Interfaces;
using AuditTrail.API.Middleware;

// Configure Serilog before creating the builder
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "API")
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .WriteTo.File(
        path: $"logs/api-{DateTime.Now:yyyy-MM-dd}.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{Application}] {Message:lj} {Properties:j}{NewLine}{Exception}",
        restrictedToMinimumLevel: LogEventLevel.Debug)
    .WriteTo.MSSqlServer(
        connectionString: "Server=.;Database=AuditTrail;Trusted_Connection=true;TrustServerCertificate=true;",
        sinkOptions: new MSSqlServerSinkOptions
        {
            TableName = "APILogs",
            SchemaName = "logging",
            AutoCreateSqlTable = false
        },
        restrictedToMinimumLevel: LogEventLevel.Information)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Use Serilog for logging
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HttpContextAccessor for getting current user
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

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        ClockSkew = TimeSpan.Zero
    };
});

// Add authorization
builder.Services.AddAuthorization();

// Configure CORS for local development
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDevelopment",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add custom middleware early in pipeline
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseCors("LocalDevelopment");

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

app.MapControllers();

try
{
    Log.Information("Starting AuditTrail API application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "AuditTrail API application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class public for testing
public partial class Program { }
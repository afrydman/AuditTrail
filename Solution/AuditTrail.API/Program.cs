using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AuditTrail.Infrastructure.Data;
using AuditTrail.Infrastructure.Repositories;
using AuditTrail.Infrastructure.Interceptors;
using AuditTrail.Core.Interfaces;

var builder = WebApplication.CreateBuilder(args);

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

app.Run();
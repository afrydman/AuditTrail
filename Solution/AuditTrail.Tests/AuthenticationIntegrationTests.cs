using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using AuditTrail.API;
using AuditTrail.Core.DTOs;
using AuditTrail.Core.Entities.Auth;
using AuditTrail.Infrastructure.Data;
using Moq;
using AuditTrail.Core.Interfaces;

namespace AuditTrail.Tests;

public class AuthenticationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthenticationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace with real SQL Server database configuration to match production
                services.RemoveAll(typeof(DbContextOptions<AuditTrailDbContext>));
                services.RemoveAll(typeof(AuditTrailDbContext));
                services.RemoveAll(typeof(IDapperContext));
                services.RemoveAll(typeof(IConfiguration));

                // Create test configuration using real database
                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        {"ConnectionStrings:DefaultConnection", "Server=.;Database=AuditTrail;Trusted_Connection=true;TrustServerCertificate=true;"},
                        {"JwtSettings:Secret", "ThisIsATestSecretKeyThatIsLongEnoughForJWT12345"},
                        {"JwtSettings:Issuer", "TestIssuer"},
                        {"JwtSettings:Audience", "TestAudience"},
                        {"JwtSettings:ExpirationInMinutes", "30"}
                    })
                    .Build();
                
                services.AddSingleton<IConfiguration>(config);

                // Add real SQL Server database context
                services.AddDbContext<AuditTrailDbContext>(options =>
                {
                    options.UseSqlServer("Server=.;Database=AuditTrail;Trusted_Connection=true;TrustServerCertificate=true;");
                });

                services.AddSingleton<IDapperContext, DapperContext>();
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "admin",
            Password = "admin123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Debug: Print the raw response to see what's happening
        System.Console.WriteLine($"Response content: {content}");
        
        var result = JsonSerializer.Deserialize<Result<LoginResponse>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        if (!result.IsSuccess)
        {
            System.Console.WriteLine($"Login failed: {result.ErrorMessage}");
        }
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("admin", result.Data.User.Username);
        Assert.Equal("admin@audittrail.local", result.Data.User.Email);
        Assert.NotNull(result.Data.Token);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsFailure()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "admin",
            Password = "wrongpassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.True(response.IsSuccessStatusCode); // API returns 200 even for failed auth
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Result<LoginResponse>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid username or password", result.ErrorMessage);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "nonexistent",
            Password = "anypassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Result<LoginResponse>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid username or password", result.ErrorMessage);
    }

    [Fact]
    public void BCryptHash_FromDatabase_VerifiesCorrectly()
    {
        // This test validates that the exact hash from the database works with our password
        var storedHash = "$2a$11$.gYR1V/nyTae4Q9KDlLgxekEp2yNi9j8Ik/hsixB15GEyAAuomuJa";
        var password = "admin123";

        var isValid = BCrypt.Net.BCrypt.Verify(password, storedHash);
        
        Assert.True(isValid);
    }
}
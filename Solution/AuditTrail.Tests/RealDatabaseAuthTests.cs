using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;
using AuditTrail.Infrastructure.Data;
using AuditTrail.Infrastructure.Repositories;

namespace AuditTrail.Tests;

public class RealDatabaseAuthTests : IDisposable
{
    private readonly AuditTrailDbContext _context;
    private readonly DapperContext _dapperContext;
    private readonly UserRepository _userRepository;

    public RealDatabaseAuthTests()
    {
        // Use real database connections
        var connectionString = "Server=.;Database=AuditTrail;Trusted_Connection=true;TrustServerCertificate=true;";
        
        var options = new DbContextOptionsBuilder<AuditTrailDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        _context = new AuditTrailDbContext(options);
        
        // Create configuration for DapperContext
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"ConnectionStrings:DefaultConnection", connectionString}
            })
            .Build();
            
        _dapperContext = new DapperContext(config);
        _userRepository = new UserRepository(_context, _dapperContext);
    }

    [Fact]
    public async Task AuthenticateAsync_WithRealDatabase_ValidCredentials_ReturnsUser()
    {
        // Act
        var result = await _userRepository.AuthenticateAsync("admin", "admin123", "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("admin", result.Username);
        Assert.Equal("admin@audittrail.local", result.Email);
        Console.WriteLine($"Authentication successful for user: {result.Username}");
    }

    [Fact]
    public async Task AuthenticateAsync_WithRealDatabase_InvalidPassword_ReturnsNull()
    {
        // Act
        var result = await _userRepository.AuthenticateAsync("admin", "wrongpassword", "127.0.0.1");

        // Assert
        Assert.Null(result);
        Console.WriteLine("Authentication correctly failed for invalid password");
    }

    [Fact]
    public async Task GetByUsernameAsync_WithRealDatabase_FindsAdminUser()
    {
        // Act
        var user = await _userRepository.GetByUsernameAsync("admin");

        // Assert
        Assert.NotNull(user);
        Assert.Equal("admin", user.Username);
        Assert.Equal("admin@audittrail.local", user.Email);
        
        // Test BCrypt verification
        var isValidPassword = BCrypt.Net.BCrypt.Verify("admin123", user.PasswordHash);
        Assert.True(isValidPassword);
        
        Console.WriteLine($"Found user: {user.Username}, Hash length: {user.PasswordHash.Length}");
        Console.WriteLine($"BCrypt verification: {isValidPassword}");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
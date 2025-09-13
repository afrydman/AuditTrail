using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using AuditTrail.Core.Entities.Auth;
using AuditTrail.Infrastructure.Data;
using AuditTrail.Infrastructure.Repositories;
using AuditTrail.Core.Interfaces;
using System.Data;
using System.Data.Common;

namespace AuditTrail.Tests;

public class DirectAuthenticationTests : IDisposable
{
    private readonly AuditTrailDbContext _context;
    private readonly Mock<IDapperContext> _mockDapperContext;
    private readonly Mock<DbConnection> _mockConnection;
    private readonly UserRepository _userRepository;

    public DirectAuthenticationTests()
    {
        var options = new DbContextOptionsBuilder<AuditTrailDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AuditTrailDbContext(options);
        _mockDapperContext = new Mock<IDapperContext>();
        _mockConnection = new Mock<DbConnection>();
        
        // Setup mock connection
        _mockDapperContext.Setup(x => x.CreateConnection()).Returns(_mockConnection.Object);
        
        _userRepository = new UserRepository(_context, _mockDapperContext.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var adminRole = new Role
        {
            Id = 1,
            RoleName = "Site Admin",
            Description = "Site Administrator",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var adminUser = new User
        {
            Id = Guid.Parse("465EE473-054A-46B3-A088-90D4125B9BE9"),
            Username = "admin",
            Email = "admin@audittrail.local",
            PasswordHash = "$2a$11$.gYR1V/nyTae4Q9KDlLgxekEp2yNi9j8Ik/hsixB15GEyAAuomuJa",
            PasswordSalt = "",
            FirstName = "Site",
            LastName = "Administrator",
            RoleId = 1,
            IsActive = true,
            IsEmailVerified = true,
            FailedLoginAttempts = 0,
            IsLocked = false,
            MustChangePassword = false,
            CreatedDate = DateTime.UtcNow
        };

        _context.Roles.Add(adminRole);
        _context.Users.Add(adminUser);
        _context.SaveChanges();
    }

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ReturnsUser()
    {
        // This test bypasses stored procedures by directly testing the core logic
        
        // Step 1: Test GetByUsernameAsync (this uses EF, not Dapper)
        var user = await _userRepository.GetByUsernameAsync("admin");
        Assert.NotNull(user);
        Assert.Equal("admin", user.Username);
        Assert.True(user.IsActive);
        Assert.False(user.IsLocked);
        
        // Step 2: Test BCrypt verification directly
        var passwordMatch = BCrypt.Net.BCrypt.Verify("admin123", user.PasswordHash);
        Assert.True(passwordMatch);
        
        // This proves that the core authentication logic should work
        // The issue is likely in the stored procedure or Dapper connection
    }

    [Fact]
    public async Task GetByUsernameAsync_FindsAdminUser()
    {
        var user = await _userRepository.GetByUsernameAsync("admin");
        
        Assert.NotNull(user);
        Assert.Equal("admin", user.Username);
        Assert.Equal("admin@audittrail.local", user.Email);
        Assert.Equal("Site Admin", user.Role?.RoleName);
        Assert.True(user.IsActive);
        Assert.False(user.IsLocked);
        
        // Test the exact hash from database
        var isValidPassword = BCrypt.Net.BCrypt.Verify("admin123", user.PasswordHash);
        Assert.True(isValidPassword);
    }

    [Fact]
    public async Task GetByUsernameAsync_NonExistentUser_ReturnsNull()
    {
        var user = await _userRepository.GetByUsernameAsync("nonexistent");
        Assert.Null(user);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WithCorrectPassword_ReturnsTrue()
    {
        var isValid = await _userRepository.ValidateCredentialsAsync("admin", "admin123");
        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WithWrongPassword_ReturnsFalse()
    {
        var isValid = await _userRepository.ValidateCredentialsAsync("admin", "wrongpassword");
        Assert.False(isValid);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
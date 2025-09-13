using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using AuditTrail.Core.Entities.Auth;
using AuditTrail.Infrastructure.Data;
using AuditTrail.Infrastructure.Repositories;
using AuditTrail.Core.Interfaces;

namespace AuditTrail.Tests;

public class SimpleAuthenticationTests : IDisposable
{
    private readonly AuditTrailDbContext _context;
    private readonly Mock<IDapperContext> _mockDapperContext;
    private readonly UserRepository _userRepository;

    public SimpleAuthenticationTests()
    {
        var options = new DbContextOptionsBuilder<AuditTrailDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AuditTrailDbContext(options);
        _mockDapperContext = new Mock<IDapperContext>();
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
            PasswordHash = "$2a$11$.gYR1V/nyTae4Q9KDlLgxekEp2yNi9j8Ik/hsixB15GEyAAuomuJa", // admin123
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
    public async Task AuthenticateAsync_ValidCredentials_ReturnsUser()
    {
        // Act
        var result = await _userRepository.AuthenticateAsync("admin", "admin123", "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("admin", result.Username);
        Assert.Equal("admin@audittrail.local", result.Email);
        
        // Check that last login was updated
        Assert.NotNull(result.LastLoginDate);
        Assert.Equal("127.0.0.1", result.LastLoginIP);
        Assert.Equal(0, result.FailedLoginAttempts);
        Assert.False(result.IsLocked);
    }

    [Fact]
    public async Task AuthenticateAsync_InvalidPassword_ReturnsNull()
    {
        // Act
        var result = await _userRepository.AuthenticateAsync("admin", "wrongpassword", "127.0.0.1");

        // Assert
        Assert.Null(result);
        
        // Check that failed attempts were incremented
        var user = await _userRepository.GetByUsernameAsync("admin");
        Assert.NotNull(user);
        Assert.Equal(1, user.FailedLoginAttempts);
    }

    [Fact]
    public async Task AuthenticateAsync_NonExistentUser_ReturnsNull()
    {
        // Act
        var result = await _userRepository.AuthenticateAsync("nonexistent", "anypassword", "127.0.0.1");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AuthenticateAsync_InactiveUser_ReturnsNull()
    {
        // Arrange
        var user = await _context.Users.FirstAsync(u => u.Username == "admin");
        user.IsActive = false;
        await _context.SaveChangesAsync();

        // Act
        var result = await _userRepository.AuthenticateAsync("admin", "admin123", "127.0.0.1");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AuthenticateAsync_LockedUser_ReturnsNull()
    {
        // Arrange
        var user = await _context.Users.FirstAsync(u => u.Username == "admin");
        user.IsLocked = true;
        await _context.SaveChangesAsync();

        // Act
        var result = await _userRepository.AuthenticateAsync("admin", "admin123", "127.0.0.1");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AuthenticateAsync_MultipleFailedAttempts_LocksAccount()
    {
        // Act - Try wrong password 5 times
        for (int i = 0; i < 5; i++)
        {
            await _userRepository.AuthenticateAsync("admin", "wrongpassword", "127.0.0.1");
        }

        // Assert - Account should be locked
        var user = await _userRepository.GetByUsernameAsync("admin");
        Assert.NotNull(user);
        Assert.Equal(5, user.FailedLoginAttempts);
        Assert.True(user.IsLocked);

        // Try with correct password - should still fail because account is locked
        var result = await _userRepository.AuthenticateAsync("admin", "admin123", "127.0.0.1");
        Assert.Null(result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
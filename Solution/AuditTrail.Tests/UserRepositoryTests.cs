using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using AuditTrail.Core.Entities.Auth;
using AuditTrail.Infrastructure.Data;
using AuditTrail.Infrastructure.Repositories;
using AuditTrail.Core.Interfaces;

namespace AuditTrail.Tests;

public class UserRepositoryTests : IDisposable
{
    private readonly AuditTrailDbContext _context;
    private readonly Mock<IDapperContext> _mockDapperContext;
    private readonly UserRepository _userRepository;

    public UserRepositoryTests()
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
    public async Task GetByUsernameAsync_ExistingUser_ReturnsUser()
    {
        // Act
        var result = await _userRepository.GetByUsernameAsync("admin");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("admin", result.Username);
        Assert.Equal("admin@audittrail.local", result.Email);
        Assert.True(result.IsActive);
        Assert.False(result.IsLocked);
    }

    [Fact]
    public async Task GetByUsernameAsync_NonExistingUser_ReturnsNull()
    {
        // Act
        var result = await _userRepository.GetByUsernameAsync("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ValidCredentials_ReturnsTrue()
    {
        // Act
        var result = await _userRepository.ValidateCredentialsAsync("admin", "admin123");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_InvalidPassword_ReturnsFalse()
    {
        // Act
        var result = await _userRepository.ValidateCredentialsAsync("admin", "wrongpassword");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_InactiveUser_ReturnsFalse()
    {
        // Arrange
        var user = await _context.Users.FirstAsync(u => u.Username == "admin");
        user.IsActive = false;
        await _context.SaveChangesAsync();

        // Act
        var result = await _userRepository.ValidateCredentialsAsync("admin", "admin123");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_LockedUser_ReturnsFalse()
    {
        // Arrange
        var user = await _context.Users.FirstAsync(u => u.Username == "admin");
        user.IsLocked = true;
        await _context.SaveChangesAsync();

        // Act
        var result = await _userRepository.ValidateCredentialsAsync("admin", "admin123");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void BCryptVerification_WithStoredHash_ReturnsTrue()
    {
        // Arrange
        var password = "admin123";
        var storedHash = "$2a$11$.gYR1V/nyTae4Q9KDlLgxekEp2yNi9j8Ik/hsixB15GEyAAuomuJa";

        // Act
        var result = BCrypt.Net.BCrypt.Verify(password, storedHash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void BCryptVerification_WithWrongPassword_ReturnsFalse()
    {
        // Arrange
        var password = "wrongpassword";
        var storedHash = "$2a$11$.gYR1V/nyTae4Q9KDlLgxekEp2yNi9j8Ik/hsixB15GEyAAuomuJa";

        // Act
        var result = BCrypt.Net.BCrypt.Verify(password, storedHash);

        // Assert
        Assert.False(result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
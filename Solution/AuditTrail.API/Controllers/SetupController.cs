using Microsoft.AspNetCore.Mvc;
using AuditTrail.Core.Entities.Auth;
using AuditTrail.Core.Interfaces;
using AuditTrail.Core.DTOs;

namespace AuditTrail.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SetupController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<SetupController> _logger;

    public SetupController(IUserRepository userRepository, ILogger<SetupController> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// Create initial admin user - ONLY for system setup
    /// </summary>
    [HttpPost("create-admin")]
    public async Task<IActionResult> CreateAdminUser([FromBody] CreateAdminRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if any users already exist (this should only be used for initial setup)
            var existingUsers = await _userRepository.GetAllAsync();
            if (existingUsers.Any())
            {
                return BadRequest(new { Message = "Users already exist. Use the regular user creation endpoint." });
            }

            // Hash password using BCrypt
            var salt = BCrypt.Net.BCrypt.GenerateSalt();
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password, salt);

            // Create admin user
            var adminUser = new User
            {
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = hashedPassword,
                PasswordSalt = salt,
                RoleId = 1, // Admin role
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = Guid.NewGuid() // System user
            };

            var createdUser = await _userRepository.AddAsync(adminUser);
            
            _logger.LogInformation("Admin user created successfully: {Username}", adminUser.Username);

            var userDto = new UserDto
            {
                Id = createdUser.Id,
                Username = createdUser.Username,
                Email = createdUser.Email,
                FirstName = createdUser.FirstName,
                LastName = createdUser.LastName,
                RoleId = createdUser.RoleId,
                RoleName = "System Administrator",
                IsActive = createdUser.IsActive,
                CreatedDate = createdUser.CreatedDate
            };

            return Ok(new { Message = "Admin user created successfully", User = userDto });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating admin user: {Username}", request.Username);
            return StatusCode(500, new { Message = "An error occurred while creating the admin user" });
        }
    }

    /// <summary>
    /// Quick user creation endpoint for testing
    /// </summary>
    [HttpPost("create-test-user")]
    public async Task<IActionResult> CreateTestUser([FromBody] CreateTestUserRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if user already exists
            var existingUser = await _userRepository.GetByUsernameAsync(request.Username);
            if (existingUser != null)
            {
                return BadRequest(new { Message = "Username already exists" });
            }

            // Hash password using BCrypt
            var salt = BCrypt.Net.BCrypt.GenerateSalt();
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password, salt);

            // Create test user
            var testUser = new User
            {
                Username = request.Username,
                Email = request.Email ?? $"{request.Username}@audittrail.test",
                FirstName = request.FirstName ?? "Test",
                LastName = request.LastName ?? "User",
                PasswordHash = hashedPassword,
                PasswordSalt = salt,
                RoleId = request.RoleId ?? 2, // Default to regular user role
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = Guid.NewGuid() // System user
            };

            var createdUser = await _userRepository.AddAsync(testUser);
            
            _logger.LogInformation("Test user created successfully: {Username}", testUser.Username);

            var userDto = new UserDto
            {
                Id = createdUser.Id,
                Username = createdUser.Username,
                Email = createdUser.Email,
                FirstName = createdUser.FirstName,
                LastName = createdUser.LastName,
                RoleId = createdUser.RoleId,
                IsActive = createdUser.IsActive,
                CreatedDate = createdUser.CreatedDate
            };

            return Ok(new { Message = "Test user created successfully", User = userDto });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating test user: {Username}", request.Username);
            return StatusCode(500, new { Message = "An error occurred while creating the test user" });
        }
    }

    /// <summary>
    /// Get system status
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetSystemStatus()
    {
        try
        {
            var users = await _userRepository.GetAllAsync();
            var userCount = users.Count();
            var hasAdminUser = users.Any(u => u.RoleId == 1 && u.IsActive);

            return Ok(new
            {
                TotalUsers = userCount,
                HasAdminUser = hasAdminUser,
                IsInitialized = userCount > 0,
                SystemReady = hasAdminUser
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system status");
            return StatusCode(500, new { Message = "An error occurred while checking system status" });
        }
    }
}

/// <summary>
/// Request model for creating an admin user
/// </summary>
public class CreateAdminRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Request model for creating a test user
/// </summary>
public class CreateTestUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Password { get; set; } = string.Empty;
    public int? RoleId { get; set; }
}
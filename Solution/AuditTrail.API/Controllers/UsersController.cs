using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AuditTrail.Core.Entities.Auth;
using AuditTrail.Core.Interfaces;
using AuditTrail.Core.DTOs;

namespace AuditTrail.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserRepository userRepository, ILogger<UsersController> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
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

            // Check if email already exists
            var existingEmail = await _userRepository.GetByEmailAsync(request.Email);
            if (existingEmail != null)
            {
                return BadRequest(new { Message = "Email already exists" });
            }

            // Hash password
            var (hashedPassword, salt) = HashPassword(request.Password);

            // Create new user
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = hashedPassword,
                PasswordSalt = salt,
                RoleId = request.RoleId ?? 1, // Default to basic role if not specified
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = Guid.NewGuid() // For now, use a system ID
            };

            var createdUser = await _userRepository.AddAsync(user);
            
            _logger.LogInformation("User created successfully: {Username}", user.Username);

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

            return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user: {Username}", request.Username);
            return StatusCode(500, new { Message = "An error occurred while creating the user" });
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                RoleId = user.RoleId,
                IsActive = user.IsActive,
                CreatedDate = user.CreatedDate,
                LastLoginDate = user.LastLoginDate
            };

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user: {UserId}", id);
            return StatusCode(500, new { Message = "An error occurred while retrieving the user" });
        }
    }

    /// <summary>
    /// Get all users (paginated)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var users = await _userRepository.GetAllAsync();
            var userDtos = users.Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .Select(user => new UserDto
                              {
                                  Id = user.Id,
                                  Username = user.Username,
                                  Email = user.Email,
                                  FirstName = user.FirstName,
                                  LastName = user.LastName,
                                  RoleId = user.RoleId,
                                  IsActive = user.IsActive,
                                  CreatedDate = user.CreatedDate,
                                  LastLoginDate = user.LastLoginDate
                              }).ToList();

            var totalCount = users.Count();

            return Ok(new
            {
                Users = userDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return StatusCode(500, new { Message = "An error occurred while retrieving users" });
        }
    }

    /// <summary>
    /// Update user
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            // Update user properties
            user.Email = request.Email;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.IsActive = request.IsActive;
            user.ModifiedDate = DateTime.UtcNow;

            if (request.RoleId.HasValue)
            {
                user.RoleId = request.RoleId.Value;
            }

            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User updated successfully: {Username}", user.Username);

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                RoleId = user.RoleId,
                IsActive = user.IsActive,
                CreatedDate = user.CreatedDate,
                LastLoginDate = user.LastLoginDate
            };

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {UserId}", id);
            return StatusCode(500, new { Message = "An error occurred while updating the user" });
        }
    }

    /// <summary>
    /// Delete user (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            // Soft delete - just deactivate the user
            user.IsActive = false;
            user.ModifiedDate = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User deactivated: {Username}", user.Username);

            return Ok(new { Message = "User deactivated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user: {UserId}", id);
            return StatusCode(500, new { Message = "An error occurred while deleting the user" });
        }
    }

    /// <summary>
    /// Change user password
    /// </summary>
    [HttpPost("{id}/change-password")]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            // Hash new password
            var (hashedPassword, salt) = HashPassword(request.NewPassword);

            // Update password
            user.PasswordHash = hashedPassword;
            user.PasswordSalt = salt;
            user.ModifiedDate = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Password changed for user: {Username}", user.Username);

            return Ok(new { Message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user: {UserId}", id);
            return StatusCode(500, new { Message = "An error occurred while changing the password" });
        }
    }

    /// <summary>
    /// Simple password hashing method using BCrypt
    /// </summary>
    private (string hashedPassword, string salt) HashPassword(string password)
    {
        var salt = BCrypt.Net.BCrypt.GenerateSalt();
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, salt);
        return (hashedPassword, salt);
    }
}

/// <summary>
/// Request model for creating a new user
/// </summary>
public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int? RoleId { get; set; }
}

/// <summary>
/// Request model for updating a user
/// </summary>
public class UpdateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int? RoleId { get; set; }
}

/// <summary>
/// Request model for changing password
/// </summary>
public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
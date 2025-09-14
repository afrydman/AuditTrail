using AuditTrail.Core.Entities.Auth;
using AuditTrail.Core.Interfaces;
using AuditTrail.Infrastructure.Data;
using AuditTrail.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace AuditTrail.Web.Controllers;

[Authorize]
public class UsersController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly AuditTrailDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserRepository userRepository,
        AuditTrailDbContext context,
        ILogger<UsersController> logger)
    {
        _userRepository = userRepository;
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index(
        string searchTerm, 
        string sortOrder, 
        string currentFilter,
        int? pageNumber,
        int pageSize = 10)
    {
        ViewData["Title"] = "Gestión de Usuarios";
        ViewData["CurrentSort"] = sortOrder;
        ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
        ViewData["EmailSortParm"] = sortOrder == "Email" ? "email_desc" : "Email";
        ViewData["RoleSortParm"] = sortOrder == "Role" ? "role_desc" : "Role";
        ViewData["StatusSortParm"] = sortOrder == "Status" ? "status_desc" : "Status";
        ViewData["LastLoginSortParm"] = sortOrder == "LastLogin" ? "lastlogin_desc" : "LastLogin";

        if (searchTerm != null)
        {
            pageNumber = 1;
        }
        else
        {
            searchTerm = currentFilter;
        }

        ViewData["CurrentFilter"] = searchTerm;
        ViewData["PageSize"] = pageSize;

        var users = await _userRepository.GetAllWithRolesAsync();

        // Apply search filter
        if (!String.IsNullOrEmpty(searchTerm))
        {
            users = users.Where(u => 
                u.Username.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                u.FirstName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                u.LastName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (u.Role != null && u.Role.RoleName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        // Apply sorting
        users = sortOrder switch
        {
            "name_desc" => users.OrderByDescending(u => u.Username).ToList(),
            "Email" => users.OrderBy(u => u.Email).ToList(),
            "email_desc" => users.OrderByDescending(u => u.Email).ToList(),
            "Role" => users.OrderBy(u => u.Role?.RoleName).ToList(),
            "role_desc" => users.OrderByDescending(u => u.Role?.RoleName).ToList(),
            "Status" => users.OrderBy(u => u.IsActive).ToList(),
            "status_desc" => users.OrderByDescending(u => u.IsActive).ToList(),
            "LastLogin" => users.OrderBy(u => u.LastLoginDate).ToList(),
            "lastlogin_desc" => users.OrderByDescending(u => u.LastLoginDate).ToList(),
            _ => users.OrderBy(u => u.Username).ToList(),
        };

        // Create view model
        var viewModel = new UserListViewModel
        {
            Users = PaginatedList<User>.Create(users.AsQueryable(), pageNumber ?? 1, pageSize),
            SearchTerm = searchTerm,
            SortOrder = sortOrder
        };

        _logger.LogInformation("User {Username} accessed user management page", User.Identity?.Name);

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var user = await _userRepository.GetByIdWithRoleAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        return PartialView("_UserDetailsModal", user);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CreateUserViewModel
        {
            Roles = await GetRolesSelectList(),
            IsActive = true
        };
        
        return PartialView("_CreateUserModal", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Roles = await GetRolesSelectList();
            return PartialView("_CreateUserModal", model);
        }

        try
        {
            // Check if username already exists
            var existingUser = await _userRepository.GetByUsernameAsync(model.Username);
            if (existingUser != null)
            {
                ModelState.AddModelError("Username", "El nombre de usuario ya está en uso");
                model.Roles = await GetRolesSelectList();
                return PartialView("_CreateUserModal", model);
            }

            // Check if email already exists
            existingUser = await _userRepository.GetByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "El email ya está registrado");
                model.Roles = await GetRolesSelectList();
                return PartialView("_CreateUserModal", model);
            }

            // Create new user
            var salt = BCrypt.Net.BCrypt.GenerateSalt();
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                PasswordHash = hashedPassword,
                PasswordSalt = salt,
                FirstName = model.FirstName,
                LastName = model.LastName,
                RoleId = model.RoleId,
                IsActive = model.IsActive,
                IsEmailVerified = model.IsEmailVerified,
                MustChangePassword = model.MustChangePassword,
                CreatedDate = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);

            _logger.LogInformation("User {Username} created new user {NewUsername}", 
                User.Identity?.Name, user.Username);

            return Json(new { success = true, message = "Usuario creado exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return Json(new { success = false, message = "Error al crear el usuario" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var user = await _userRepository.GetByIdWithRoleAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var model = new EditUserViewModel
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            RoleId = user.RoleId,
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            IsLocked = user.IsLocked,
            MustChangePassword = user.MustChangePassword,
            CreatedDate = user.CreatedDate,
            LastLoginDate = user.LastLoginDate,
            LastLoginIP = user.LastLoginIP,
            FailedLoginAttempts = user.FailedLoginAttempts,
            LockoutEnd = user.LockoutEnd,
            Roles = await GetRolesSelectList()
        };

        return PartialView("_EditUserModal", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        if (model.ChangePassword && string.IsNullOrEmpty(model.NewPassword))
        {
            ModelState.AddModelError("NewPassword", "La nueva contraseña es requerida");
        }

        if (!ModelState.IsValid)
        {
            model.Roles = await GetRolesSelectList();
            return PartialView("_EditUserModal", model);
        }

        try
        {
            var user = await _userRepository.GetByIdAsync(model.Id);
            if (user == null)
            {
                return NotFound();
            }

            // Check if username is being changed and already exists
            if (user.Username != model.Username)
            {
                var existingUser = await _userRepository.GetByUsernameAsync(model.Username);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Username", "El nombre de usuario ya está en uso");
                    model.Roles = await GetRolesSelectList();
                    return PartialView("_EditUserModal", model);
                }
            }

            // Check if email is being changed and already exists
            if (user.Email != model.Email)
            {
                var existingUser = await _userRepository.GetByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "El email ya está registrado");
                    model.Roles = await GetRolesSelectList();
                    return PartialView("_EditUserModal", model);
                }
            }

            // Update user properties
            user.Username = model.Username;
            user.Email = model.Email;
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.RoleId = model.RoleId;
            user.IsActive = model.IsActive;
            user.IsEmailVerified = model.IsEmailVerified;
            user.MustChangePassword = model.MustChangePassword;
            user.ModifiedDate = DateTime.UtcNow;

            // Update password if requested
            if (model.ChangePassword && !string.IsNullOrEmpty(model.NewPassword))
            {
                var salt = BCrypt.Net.BCrypt.GenerateSalt();
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                user.PasswordHash = hashedPassword;
                user.PasswordSalt = salt;
                user.LastPasswordChangeDate = DateTime.UtcNow;
            }

            // Handle lock/unlock
            if (!model.IsLocked && user.IsLocked)
            {
                user.IsLocked = false;
                user.LockoutEnd = null;
                user.FailedLoginAttempts = 0;
            }

            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User {Username} updated user {UpdatedUsername}", 
                User.Identity?.Name, user.Username);

            return Json(new { success = true, message = "Usuario actualizado exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", model.Id);
            return Json(new { success = false, message = "Error al actualizar el usuario" });
        }
    }

    private async Task<IEnumerable<SelectListItem>> GetRolesSelectList()
    {
        var roles = await _context.Roles
            .Where(r => r.IsActive)
            .OrderBy(r => r.RoleName)
            .Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.RoleName
            })
            .ToListAsync();

        return roles;
    }

    [HttpPost]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "Usuario no encontrado" });
            }

            user.IsActive = !user.IsActive;
            user.ModifiedDate = DateTime.UtcNow;
            // TODO: Get actual user ID from claims
            // user.ModifiedBy = GetCurrentUserId();

            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User {Username} {Action} user {TargetUser}", 
                User.Identity?.Name, 
                user.IsActive ? "activated" : "deactivated", 
                user.Username);

            return Json(new { 
                success = true, 
                isActive = user.IsActive,
                message = user.IsActive ? "Usuario activado exitosamente" : "Usuario desactivado exitosamente"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling user status for user {UserId}", id);
            return Json(new { success = false, message = "Error al cambiar el estado del usuario" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UnlockAccount(Guid id)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "Usuario no encontrado" });
            }

            user.IsLocked = false;
            user.LockoutEnd = null;
            user.FailedLoginAttempts = 0;
            user.ModifiedDate = DateTime.UtcNow;
            // TODO: Get actual user ID from claims
            // user.ModifiedBy = GetCurrentUserId();

            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User {Username} unlocked account for user {TargetUser}", 
                User.Identity?.Name, 
                user.Username);

            return Json(new { 
                success = true, 
                message = "Cuenta desbloqueada exitosamente"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking account for user {UserId}", id);
            return Json(new { success = false, message = "Error al desbloquear la cuenta" });
        }
    }
}
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AuditTrail.Web.Models;

public class CreateUserViewModel
{
    [Required(ErrorMessage = "El nombre de usuario es requerido")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "El nombre de usuario debe tener entre 3 y 50 caracteres")]
    [RegularExpression(@"^[a-zA-Z0-9_.-]+$", ErrorMessage = "El nombre de usuario solo puede contener letras, números, puntos, guiones y guiones bajos")]
    [Display(Name = "Nombre de Usuario")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "El email no es válido")]
    [Display(Name = "Correo Electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", 
        ErrorMessage = "La contraseña debe contener al menos una mayúscula, una minúscula, un número y un carácter especial")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Contraseña")]
    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    [Display(Name = "Nombre")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es requerido")]
    [StringLength(100, ErrorMessage = "El apellido no puede exceder 100 caracteres")]
    [Display(Name = "Apellido")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El rol es requerido")]
    [Display(Name = "Rol")]
    public int RoleId { get; set; }

    [Display(Name = "Activo")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Email Verificado")]
    public bool IsEmailVerified { get; set; } = false;

    [Display(Name = "Debe Cambiar Contraseña")]
    public bool MustChangePassword { get; set; } = false;

    [Display(Name = "Enviar Email de Bienvenida")]
    public bool SendWelcomeEmail { get; set; } = true;

    public IEnumerable<SelectListItem> Roles { get; set; } = new List<SelectListItem>();
}
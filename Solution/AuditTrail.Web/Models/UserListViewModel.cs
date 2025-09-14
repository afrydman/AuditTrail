using AuditTrail.Core.Entities.Auth;

namespace AuditTrail.Web.Models;

public class UserListViewModel
{
    public PaginatedList<User> Users { get; set; } = null!;
    public string? SearchTerm { get; set; }
    public string? SortOrder { get; set; }
}
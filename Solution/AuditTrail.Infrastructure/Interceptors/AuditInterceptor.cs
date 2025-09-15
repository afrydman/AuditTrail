using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;
using AuditTrail.Core.Interfaces;
using AuditTrail.Core.Entities.Audit;

namespace AuditTrail.Infrastructure.Interceptors;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IAuditRepository _auditRepository;
    private readonly ICurrentUserService _currentUserService;

    public AuditInterceptor(IAuditRepository auditRepository, ICurrentUserService currentUserService)
    {
        _auditRepository = auditRepository;
        _currentUserService = currentUserService;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var auditEntries = CreateAuditEntries(eventData.Context);

        // Save the audit entries after the main transaction
        if (auditEntries.Any())
        {
            foreach (var auditEntry in auditEntries)
            {
                await _auditRepository.LogAuditEventAsync(new AuditTrailEntry
                {
                    EventType = auditEntry.EventType,
                    Action = auditEntry.Action,
                    UserId = _currentUserService.UserId,
                    Username = _currentUserService.Username,
                    EntityType = auditEntry.EntityType,
                    EntityId = auditEntry.EntityId,
                    EntityName = auditEntry.EntityName,
                    OldValue = auditEntry.OldValue,
                    NewValue = auditEntry.NewValue,
                    IPAddress = _currentUserService.IpAddress,
                    Result = "Success"
                });
            }
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private List<AuditEntry> CreateAuditEntries(DbContext context)
    {
        var auditEntries = new List<AuditEntry>();
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || 
                       e.State == EntityState.Modified || 
                       e.State == EntityState.Deleted)
            .Where(e => !IsAuditEntity(e.Entity.GetType()));

        foreach (var entry in entries)
        {
            var auditEntry = new AuditEntry
            {
                EntityType = entry.Entity.GetType().Name,
                Action = entry.State.ToString()
            };

            // Get the primary key value
            var keyProperties = entry.Properties
                .Where(p => p.Metadata.IsPrimaryKey());
            
            if (keyProperties.Any())
            {
                var keyValues = keyProperties.Select(p => p.CurrentValue?.ToString());
                auditEntry.EntityId = string.Join(",", keyValues);
            }

            // Determine event type based on entity and action
            auditEntry.EventType = DetermineEventType(entry.Entity.GetType().Name, entry.State);
            
            // Set entity name based on entity type
            auditEntry.EntityName = DetermineEntityName(entry.Entity, entry.State);

            // Capture old and new values for modified entities
            if (entry.State == EntityState.Modified)
            {
                var oldValues = new Dictionary<string, object?>();
                var newValues = new Dictionary<string, object?>();

                foreach (var property in entry.Properties)
                {
                    if (property.IsModified && !IsIgnoredProperty(property.Metadata.Name))
                    {
                        oldValues[property.Metadata.Name] = property.OriginalValue;
                        newValues[property.Metadata.Name] = property.CurrentValue;
                    }
                }

                if (oldValues.Any())
                {
                    auditEntry.OldValue = JsonSerializer.Serialize(oldValues);
                    auditEntry.NewValue = JsonSerializer.Serialize(newValues);
                }
            }
            else if (entry.State == EntityState.Added)
            {
                var values = new Dictionary<string, object?>();
                foreach (var property in entry.Properties)
                {
                    if (!IsIgnoredProperty(property.Metadata.Name))
                    {
                        values[property.Metadata.Name] = property.CurrentValue;
                    }
                }
                auditEntry.NewValue = JsonSerializer.Serialize(values);
            }
            else if (entry.State == EntityState.Deleted)
            {
                var values = new Dictionary<string, object?>();
                foreach (var property in entry.Properties)
                {
                    if (!IsIgnoredProperty(property.Metadata.Name))
                    {
                        values[property.Metadata.Name] = property.OriginalValue;
                    }
                }
                auditEntry.OldValue = JsonSerializer.Serialize(values);
            }

            auditEntries.Add(auditEntry);
        }

        return auditEntries;
    }

    private bool IsAuditEntity(Type entityType)
    {
        // Don't audit the audit tables themselves
        return entityType.Name.Contains("Audit") || 
               entityType.Name.Contains("LoginAttempt");
    }

    private bool IsIgnoredProperty(string propertyName)
    {
        // Ignore certain properties from audit
        var ignoredProperties = new[] { "PasswordHash", "PasswordSalt", "SessionToken", "RefreshToken" };
        return ignoredProperties.Contains(propertyName);
    }

    private string DetermineEventType(string entityName, EntityState state)
    {
        var action = state switch
        {
            EntityState.Added => "Created",
            EntityState.Modified => "Modified",
            EntityState.Deleted => "Deleted",
            _ => "Unknown"
        };

        return $"{entityName}{action}";
    }

    private string? DetermineEntityName(object entity, EntityState state)
    {
        // For deleted entities, we might not have current values, so we try to get from original values
        return entity.GetType().Name switch
        {
            "FileCategory" => GetPropertyValue(entity, "CategoryName", state),
            "FileEntity" => GetPropertyValue(entity, "FileName", state),
            "User" => GetPropertyValue(entity, "Username", state),
            _ => null
        };
    }

    private string? GetPropertyValue(object entity, string propertyName, EntityState state)
    {
        try
        {
            var property = entity.GetType().GetProperty(propertyName);
            return property?.GetValue(entity)?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private class AuditEntry
    {
        public string EventType { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string? EntityName { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }
}

// Service to get current user information
public interface ICurrentUserService
{
    Guid? UserId { get; set; }
    string? Username { get; set; }
    string? IpAddress { get; set; }
}

public class CurrentUserService : ICurrentUserService
{
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
    public string? IpAddress { get; set; }
}
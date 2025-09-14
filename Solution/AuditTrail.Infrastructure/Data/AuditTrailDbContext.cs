using Microsoft.EntityFrameworkCore;
using AuditTrail.Core.Entities;
using AuditTrail.Core.Entities.Auth;
using AuditTrail.Core.Entities.Documents;
using AuditTrail.Core.Entities.Audit;

namespace AuditTrail.Infrastructure.Data;

public class AuditTrailDbContext : DbContext
{
    public AuditTrailDbContext(DbContextOptions<AuditTrailDbContext> options) : base(options)
    {
    }

    // Auth tables
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }
    
    // Document tables
    public DbSet<FileEntity> Files { get; set; }
    public DbSet<FileCategory> FileCategories { get; set; }
    public DbSet<FileVersion> FileVersions { get; set; }
    public DbSet<CategoryAccess> CategoryAccesses { get; set; }
    
    // Audit tables
    public DbSet<AuditTrailEntry> AuditTrail { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set default schema for each area
        modelBuilder.Entity<User>().ToTable("Users", "auth");
        modelBuilder.Entity<Role>().ToTable("Roles", "auth");
        modelBuilder.Entity<UserSession>().ToTable("UserSessions", "auth");
        modelBuilder.Entity<Permission>().ToTable("Permissions", "auth");
        modelBuilder.Entity<RolePermission>().ToTable("RolePermissions", "auth");
        modelBuilder.Entity<UserPermission>().ToTable("UserPermissions", "auth");
        
        modelBuilder.Entity<FileEntity>().ToTable("Files", "docs");
        modelBuilder.Entity<FileCategory>().ToTable("FileCategories", "docs");
        modelBuilder.Entity<FileVersion>().ToTable("FileVersions", "docs");
        modelBuilder.Entity<CategoryAccess>().ToTable("CategoryAccess", "docs");
        
        modelBuilder.Entity<AuditTrailEntry>().ToTable("AuditTrail", "audit");

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("UserId");
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            
            entity.HasOne(e => e.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(e => e.RoleId);
        });

        // Configure Role entity
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("RoleId");
            entity.Property(e => e.RoleName).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.RoleName).IsUnique();
        });

        // Configure FileEntity
        modelBuilder.Entity<FileEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("FileId");
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            
            // FileEntity uses UploadedDate/UploadedBy instead of CreatedDate/CreatedBy
            // And doesn't have ModifiedDate/ModifiedBy columns in the database
            entity.Ignore(e => e.CreatedDate);
            entity.Ignore(e => e.CreatedBy);
            entity.Ignore(e => e.ModifiedDate);
            entity.Ignore(e => e.ModifiedBy);
            
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Files)
                .HasForeignKey(e => e.CategoryId);
                
            entity.HasIndex(e => new { e.FilePath, e.FileName, e.Version }).IsUnique();
        });

        // Configure FileCategory
        modelBuilder.Entity<FileCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("CategoryId");
            entity.Property(e => e.CategoryPath).IsRequired().HasMaxLength(500);
            entity.HasIndex(e => e.CategoryPath).IsUnique();
            
            // FileCategories table doesn't have ModifiedDate/ModifiedBy columns - ignore them
            // Also ignore CreatedDate since database handles it with SYSUTCDATETIME() default
            entity.Ignore(e => e.CreatedDate);
            entity.Ignore(e => e.ModifiedDate);
            entity.Ignore(e => e.ModifiedBy);
            
            entity.HasOne(e => e.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(e => e.ParentCategoryId);
        });

        // Configure FileVersion
        modelBuilder.Entity<FileVersion>(entity =>
        {
            entity.HasKey(e => e.VersionId);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.BlobUrl).HasMaxLength(500);
            entity.Property(e => e.S3Key).HasMaxLength(500);
            entity.Property(e => e.Checksum).HasMaxLength(100);
            
            entity.HasOne(e => e.File)
                .WithMany()
                .HasForeignKey(e => e.FileId);
                
            entity.HasIndex(e => new { e.FileId, e.Version }).IsUnique();
        });

        // Configure AuditTrailEntry - IMMUTABLE table
        modelBuilder.Entity<AuditTrailEntry>(entity =>
        {
            entity.HasKey(e => e.AuditId);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            
            // Indexes for performance
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
        });

        // Configure UserSession
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.SessionId);
            entity.Property(e => e.SessionToken).IsRequired().HasMaxLength(500);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.Sessions)
                .HasForeignKey(e => e.UserId);
        });

        // Configure CategoryAccess
        modelBuilder.Entity<CategoryAccess>(entity =>
        {
            entity.HasKey(e => e.CategoryAccessId);
            
            entity.HasOne(e => e.Category)
                .WithMany(c => c.CategoryAccesses)
                .HasForeignKey(e => e.CategoryId);
                
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId);
                
            entity.HasOne(e => e.Role)
                .WithMany()
                .HasForeignKey(e => e.RoleId);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update timestamps for BaseEntity
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;
            var entityType = entry.Entity.GetType();
            
            if (entry.State == EntityState.Added)
            {
                // Skip entities that don't use CreatedDate (FileCategory uses database default, FileEntity uses UploadedDate)
                if (entityType.Name != nameof(FileCategory) && entityType.Name != nameof(FileEntity))
                {
                    entity.CreatedDate = DateTime.UtcNow;
                }
            }
            
            if (entry.State == EntityState.Modified)
            {
                // Skip entities that don't use ModifiedDate (FileCategory and FileEntity don't support it in DB)
                if (entityType.Name != nameof(FileCategory) && entityType.Name != nameof(FileEntity))
                {
                    entity.ModifiedDate = DateTime.UtcNow;
                }
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
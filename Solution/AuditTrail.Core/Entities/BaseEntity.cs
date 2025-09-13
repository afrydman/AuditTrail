namespace AuditTrail.Core.Entities;

public abstract class BaseEntity
{
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public Guid? ModifiedBy { get; set; }
}

public abstract class BaseEntityWithId : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}

public abstract class BaseEntityWithIntId : BaseEntity
{
    public int Id { get; set; }
}
namespace ERP.Core.Domain.Common;

public abstract class BaseEntity
{
    public int Id { get; set; }
}

/// <summary>
/// Oluşturma / güncelleme bilgisi taşıyan entity. Alanlar SaveChanges sırasında
/// server zamanı ve aktif kullanıcı ile otomatik doldurulur; elle set edilmez.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    // Bu bilgiler audit kaydının kendisinde zaten bulunur.
    [AuditIgnore] public DateTime CreatedAt { get; set; }
    [AuditIgnore] public string? CreatedBy { get; set; }
    [AuditIgnore] public DateTime? UpdatedAt { get; set; }
    [AuditIgnore] public string? UpdatedBy { get; set; }
}

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}

/// <summary>Master data için temel sınıf: audit alanları + soft delete.</summary>
public abstract class SoftDeletableEntity : AuditableEntity, ISoftDeletable
{
    public bool IsDeleted { get; set; }
    [AuditIgnore] public DateTime? DeletedAt { get; set; }
    [AuditIgnore] public string? DeletedBy { get; set; }
}

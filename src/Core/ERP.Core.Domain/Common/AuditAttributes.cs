namespace ERP.Core.Domain.Common;

/// <summary>Bu entity'deki değişiklikler audit log'a yazılmaz (ör. kendisi log / token tablosu).</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class NotAuditedAttribute : Attribute;

/// <summary>Alan audit log'da yer almaz; yalnızca bu alanlar değiştiyse kayıt da oluşmaz.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AuditIgnoreAttribute : Attribute;

/// <summary>Alanın değiştiği kaydedilir ama değeri yazılmaz (parola hash'i gibi).</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AuditMaskedAttribute : Attribute;

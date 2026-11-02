using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ERP.Core.Infrastructure.Persistence.Converters;

/// <summary>
/// SQL Server datetime2 saat dilimi bilgisi taşımaz; okunan değer Kind=Unspecified gelir ve
/// JSON'a "Z" olmadan yazılır (istemci yerel saat sanar). Sistemde tüm zamanlar UTC'dir.
/// </summary>
public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value,
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

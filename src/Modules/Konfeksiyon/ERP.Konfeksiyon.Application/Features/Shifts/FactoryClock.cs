using ERP.Konfeksiyon.Application.Features.WorkSessions;
using Microsoft.Extensions.Options;

namespace ERP.Konfeksiyon.Application.Features.Shifts;

/// <summary>
/// Fabrikanın saati. Veritabanındaki zamanlar UTC'dir; vardiya ve mola saatleri ise fabrikanın yerel
/// saatine göre tanımlanır. Dönüşüm tek yerden, ayarlı saat dilimiyle yapılır (sunucunun saat dilimine
/// bağlı değildir).
/// </summary>
public interface IFactoryClock
{
    DateTime UtcNow { get; }

    /// <summary>UTC anının fabrikadaki yerel karşılığı.</summary>
    DateTime ToLocal(DateTime utc);

    /// <summary>Fabrikanın yerel saatindeki bir anın UTC karşılığı.</summary>
    DateTime ToUtc(DateTime local);
}

public sealed class FactoryClock(TimeProvider timeProvider, IOptions<ProductionOptions> options) : IFactoryClock
{
    private readonly TimeZoneInfo _zone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);

    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), _zone);

    public DateTime ToUtc(DateTime local)
    {
        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        // Yaz saatine geçişte atlanan (var olmayan) saat bir sonraki geçerli ana kaydırılır.
        return TimeZoneInfo.ConvertTimeToUtc(
            _zone.IsInvalidTime(unspecified) ? unspecified.AddHours(1) : unspecified, _zone);
    }
}

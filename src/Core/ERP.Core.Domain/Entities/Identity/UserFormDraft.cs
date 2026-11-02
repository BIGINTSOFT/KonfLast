using ERP.Core.Domain.Common;

namespace ERP.Core.Domain.Entities.Identity;

/// <summary>
/// Kullanıcının yarım kalan formu (ör. kaydedilmemiş müşteri siparişi). Sayfa yenilenir ya da bağlantı koparsa
/// form kaldığı yerden açılır. Yalnızca o kullanıcı görür; içerik web'in form durumudur (JSON), sunucu yorumlamaz.
/// </summary>
[NotAudited]
public class UserFormDraft : BaseEntity
{
    public const int KeyMaxLength = 100;
    public const int MaxPayloadLength = 512 * 1024;
    public const int MaxPerUser = 50;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Formun anahtarı, örn. <c>sales-order:new</c> ya da <c>sales-order:42</c>.</summary>
    public string FormKey { get; set; } = null!;

    /// <summary>Formun JSON durumu.</summary>
    public string Payload { get; set; } = null!;

    /// <summary>Taslakta gösterilen kısa açıklama (ör. müşteri adı).</summary>
    public string? Title { get; set; }

    public DateTime UpdatedAt { get; set; }
}

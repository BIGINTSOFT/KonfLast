using ERP.Core.Domain.Common;

namespace ERP.Core.Domain.Entities.Identity;

/// <summary>
/// Kullanıcının ana sayfasındaki kısayol: sık kullandığı bir ekran. Hangi ekranların var olduğunu ve
/// kullanıcının yetkisini web belirler; burada yalnızca ekranın adresi ve sırası saklanır.
/// </summary>
[NotAudited]
public class UserShortcut : BaseEntity
{
    public const int MaxPerUser = 24;
    public const int PathMaxLength = 100;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Web'deki ekranın adresi, örn. <c>/production-orders</c>.</summary>
    public string Path { get; set; } = null!;

    public int SortOrder { get; set; }
}

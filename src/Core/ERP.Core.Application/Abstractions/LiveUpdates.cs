namespace ERP.Core.Application.Abstractions;

/// <summary>Canlı ekranlara bildirilen değişiklik alanları. İstemci hangi veriyi yenileyeceğini buradan anlar.</summary>
public enum LiveTopic
{
    /// <summary>Makine durumu ve bant-makine yerleşimi.</summary>
    Machines,

    /// <summary>Çalışma oturumları, mola / ara / bekleme kayıtları.</summary>
    Sessions,

    /// <summary>Demetler, demet operasyonları ve üretim olayları (adet, ara stok).</summary>
    Production,

    /// <summary>Kalite kontrolleri ve tamir işleri.</summary>
    Quality,

    /// <summary>Üretim siparişleri ve bant planları.</summary>
    Planning,

    /// <summary>Stok bakiyeleri ve stok hareketleri.</summary>
    Stock,
}

/// <summary>
/// Kalıcı hale gelmiş (commit edilmiş) değişiklikleri canlı ekranlara duyurur. Yalnızca "ne değişti"
/// bilgisi taşınır; veri taşınmaz, istemci güncel veriyi yetkisiyle API'den okur.
/// Çağrı asla bloklamaz ve hata fırlatmaz: bildirim, üretim işlemini etkileyemez.
/// </summary>
public interface ILiveNotifier
{
    void Publish(IReadOnlyCollection<LiveTopic> topics);
}

/// <summary>Bir modülün hangi entity değişikliğinin hangi canlı alanı etkilediğini bildirmesi.</summary>
public interface ILiveTopicSource
{
    IReadOnlyDictionary<Type, LiveTopic> Topics { get; }
}

/// <summary>Canlı yayın olmayan ortamlar (araçlar) için.</summary>
public sealed class NullLiveNotifier : ILiveNotifier
{
    public void Publish(IReadOnlyCollection<LiveTopic> topics)
    {
    }
}

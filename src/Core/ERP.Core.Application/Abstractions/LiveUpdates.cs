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
    /// <param name="topics">Değişen alanlar (web ekranları).</param>
    /// <param name="targets">Değişikliğin ait olduğu makine / tabletler (yalnızca ilgili tabletlere duyurulur).</param>
    void Publish(IReadOnlyCollection<LiveTopic> topics, IReadOnlyCollection<LiveTarget>? targets = null);
}

/// <summary>Bir modülün hangi entity değişikliğinin hangi canlı alanı etkilediğini bildirmesi.</summary>
public interface ILiveTopicSource
{
    IReadOnlyDictionary<Type, LiveTopic> Topics { get; }
}

public enum LiveTargetKind
{
    /// <summary>Makine (tezgâh, istasyon): o makineye bağlı tabletler.</summary>
    Machine,

    /// <summary>Doğrudan tablet (ör. tablet kaydı değişti).</summary>
    Tablet,
}

/// <summary>Değişikliğin duyurulacağı istasyon. Modül, tablet token'ındaki modül adıdır (ör. "Konfeksiyon").</summary>
public readonly record struct LiveTarget(string Module, LiveTargetKind Kind, int Id);

/// <summary>
/// Bir modülün değişen kaydın hangi makineye / tablete ait olduğunu bildirmesi. Tablet ekranı, yalnızca kendi
/// makinesindeki değişiklik duyurulunca yenilenir (web'den elle bildirim, arıza kapatma, oturum kapatma...).
/// </summary>
public interface ILiveTargetSource
{
    /// <summary>Kaydın ait olduğu hedefler; tablet ekranını ilgilendirmeyen kayıtta boştur.</summary>
    IEnumerable<LiveTarget> TargetsOf(object entity);
}

/// <summary>Bir modülde makinelere bağlı tabletleri bulur (duyuru anında; tablet başka makineye taşınabilir).</summary>
public interface ILiveTabletDirectory
{
    string Module { get; }

    Task<IReadOnlyList<int>> TabletsOfMachinesAsync(IReadOnlyCollection<int> machineIds, CancellationToken cancellationToken);
}

/// <summary>Canlı yayın olmayan ortamlar (araçlar) için.</summary>
public sealed class NullLiveNotifier : ILiveNotifier
{
    public void Publish(IReadOnlyCollection<LiveTopic> topics, IReadOnlyCollection<LiveTarget>? targets = null)
    {
    }
}

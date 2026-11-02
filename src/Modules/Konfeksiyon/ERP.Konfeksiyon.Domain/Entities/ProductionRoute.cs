using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Konfeksiyon.Domain.Routing;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>
/// Bir modelin üretim operasyonları ve aralarındaki bağımlılıklar. Rota sürümlüdür: taslakken
/// düzenlenir, etkinleştirilince kilitlenir; model başına tek etkin rota bulunur.
/// </summary>
public class ProductionRoute : SoftDeletableEntity
{
    public int ModelId { get; set; }
    public Model Model { get; set; } = null!;

    /// <summary>Model içinde 1'den başlayan sürüm numarası.</summary>
    public int Version { get; set; }

    public string? Name { get; set; }
    public string? Description { get; set; }
    public RouteStatus Status { get; private set; } = RouteStatus.Draft;

    /// <summary>Aynı rotanın iki kullanıcı tarafından eşzamanlı değiştirilmesini engeller.</summary>
    public byte[] RowVersion { get; set; } = null!;

    public ICollection<ProductionRouteOperation> Operations { get; } = new List<ProductionRouteOperation>();

    /// <summary>Operasyonlar ve bağımlılıklar yalnızca taslakken değiştirilebilir.</summary>
    public bool IsEditable => Status == RouteStatus.Draft;

    /// <summary>Taslak ya da arşivdeki rota etkinleştirilebilir.</summary>
    public bool CanActivate => Status is RouteStatus.Draft or RouteStatus.Archived;

    public bool CanArchive => Status == RouteStatus.Active;

    public void Activate()
    {
        if (!CanActivate)
        {
            throw new InvalidOperationException($"{Status} durumundaki rota etkinleştirilemez.");
        }

        Status = RouteStatus.Active;
    }

    public void Archive()
    {
        if (!CanArchive)
        {
            throw new InvalidOperationException($"{Status} durumundaki rota arşivlenemez.");
        }

        Status = RouteStatus.Archived;
    }

    /// <summary>Operasyonların bağımlılık grafiği (operasyonlar ve öncülleri yüklenmiş olmalıdır).</summary>
    public RouteGraph BuildGraph() => RouteGraph.Analyze(Operations
        .Select(o => new RouteNode(o.SequenceNo, o.Predecessors.Select(d => d.Predecessor.SequenceNo).ToList()))
        .ToList());
}

/// <summary>Rotadaki bir operasyon adımı.</summary>
public class ProductionRouteOperation : BaseEntity
{
    public int ProductionRouteId { get; set; }
    public ProductionRoute ProductionRoute { get; set; } = null!;

    /// <summary>Rota içindeki sıra / adım numarası (20, 30, 40 ...); rota içinde benzersizdir.</summary>
    public int SequenceNo { get; set; }

    public int OperationId { get; set; }
    public Operation Operation { get; set; } = null!;

    /// <summary>Bu model için standart süre (dakika / adet).</summary>
    public decimal Sam { get; set; }

    /// <summary>
    /// Adım fasonda (dışarıda) yapılır. Maliyet ve teklifte işçilik yerine fason fiyatı sayılır; demetler fason
    /// gönderim fişiyle gönderilir. Akışı ve SAM'i değiştirmediği için etkin rotada da değiştirilebilir.
    /// </summary>
    public bool IsSubcontracted { get; set; }

    /// <summary>Fason adımın varsayılan fasoncusu (gönderim fişinde ve maliyet hesabında önerilir).</summary>
    public int? SubcontractorId { get; set; }
    public Supplier? Subcontractor { get; set; }

    /// <summary>Bu adım başlamadan önce tamamlanması gereken adımlar.</summary>
    public ICollection<OperationDependency> Predecessors { get; } = new List<OperationDependency>();

    public ICollection<OperationDependency> Successors { get; } = new List<OperationDependency>();
}

/// <summary>"Öncül tamamlanmadan ardıl başlayamaz" ilişkisi; ikisi de aynı rotanın adımıdır.</summary>
public class OperationDependency
{
    public int RouteOperationId { get; set; }
    public ProductionRouteOperation RouteOperation { get; set; } = null!;

    public int PredecessorId { get; set; }
    public ProductionRouteOperation Predecessor { get; set; } = null!;
}

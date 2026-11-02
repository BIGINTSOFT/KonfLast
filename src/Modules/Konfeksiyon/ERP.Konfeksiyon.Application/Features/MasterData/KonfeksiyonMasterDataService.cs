using ERP.Core.Application.Features.MasterData;
using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Application.Abstractions;

namespace ERP.Konfeksiyon.Application.Features.MasterData;

/// <summary>Konfeksiyon tablolarını yöneten ana veri servislerinin tabanı: <see cref="Db"/> modül tablolarını da görür.</summary>
public abstract class KonfeksiyonMasterDataService<TEntity, TDto, TRequest>(IKonfeksiyonDbContext db)
    : MasterDataService<TEntity, TDto, TRequest>(db)
    where TEntity : SoftDeletableEntity, IActivatable, new()
{
    protected new IKonfeksiyonDbContext Db { get; } = db;
}

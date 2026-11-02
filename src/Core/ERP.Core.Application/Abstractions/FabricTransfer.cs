using ERP.Core.Application.Common;

namespace ERP.Core.Application.Abstractions;

/// <param name="SourceRollNo">Gönderen modülün top numarası; alan modülde müşteri top numarası olarak görünür.</param>
/// <param name="Meters">Ölçülen metre; boşsa alan modül kilodan hesaplar ve ölçülmedi diye işaretler.</param>
public sealed record FabricTransferRoll(string SourceRollNo, decimal? Meters, decimal? Kilograms);

/// <param name="CustomerId">Alan modüldeki malın sahibi; boşsa işletme malı.</param>
/// <param name="FabricId">Alan modüldeki kumaş kartı.</param>
/// <param name="SourceDocumentNo">Gönderen modülün belge numarası; alan belgede irsaliye no olarak yazılır.</param>
/// <param name="SourceNote">Alan belgeye yazılan not; kumaşın nereden geldiğini söyler.</param>
public sealed record FabricTransferRequest(
    DateOnly Date,
    int? CustomerId,
    int FabricId,
    string SourceDocumentNo,
    IReadOnlyList<FabricTransferRoll> Rolls,
    string SourceNote);

/// <summary>
/// Üretilen ham kumaşı boyaya alan modülün genişleme noktası. Modüller birbirine bağlı değildir: alan modül lisanslı ve yüklü
/// değilse bu arayüzün kaydı yoktur, gönderen modül özelliği göstermez.
/// </summary>
public interface IFabricTransferReceiver
{
    /// <summary>Kumaşı kabul eder; oluşan belgenin numarasını döner.</summary>
    Task<Result<string>> ReceiveAsync(FabricTransferRequest request, CancellationToken cancellationToken);
}

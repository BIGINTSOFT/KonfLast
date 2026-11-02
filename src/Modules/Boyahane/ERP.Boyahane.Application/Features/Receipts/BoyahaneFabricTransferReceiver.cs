using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;

namespace ERP.Boyahane.Application.Features.Receipts;

/// <summary>Dokuma ya da Örme'den gelen kumaş: normal kumaş kabul belgesi oluşur, ham kontrol kuralı aynen uygulanır.</summary>
public sealed class BoyahaneFabricTransferReceiver(IBoyahaneFabricReceiptService receipts) : IFabricTransferReceiver
{
    public async Task<Result<string>> ReceiveAsync(FabricTransferRequest request, CancellationToken cancellationToken)
    {
        var result = await receipts.CreateAsync(
            new CreateBoyahaneFabricReceiptRequest(
                request.Date,
                [new BoyahaneFabricLotRequest(request.FabricId, request.Rolls.Select(r => new BoyahaneFabricRollRequest(r.SourceRollNo, r.Meters, r.Kilograms)).ToList())],
                request.CustomerId,
                request.SourceDocumentNo,
                request.SourceNote),
            cancellationToken);

        if (result.IsSuccess)
        {
            return result.Value!.ReceiptNo;
        }

        return result.Error!;
    }
}

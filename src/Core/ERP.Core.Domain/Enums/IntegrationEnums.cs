namespace ERP.Core.Domain.Enums;

public enum IntegrationDeliveryStatus
{
    /// <summary>Gönderilmeyi (ya da yeniden denenmeyi) bekliyor.</summary>
    Pending,
    Succeeded,

    /// <summary>Deneme hakkı tükendi; kullanıcı yeniden kuyruğa alana kadar gönderilmez.</summary>
    Failed,
}

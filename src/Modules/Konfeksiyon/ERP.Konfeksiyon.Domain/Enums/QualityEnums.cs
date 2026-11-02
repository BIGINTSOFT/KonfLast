namespace ERP.Konfeksiyon.Domain.Enums;

public enum QualityResult
{
    Passed,

    /// <summary>Parçalar ıskartaya ayrıldı.</summary>
    Rejected,

    /// <summary>Parçalar tamire gönderildi.</summary>
    Rework,

    /// <summary>Demet kalite bekletmesine alındı.</summary>
    Hold,
}

public enum ReworkStatus
{
    /// <summary>Parçalar operasyonda yeniden işlenmeyi bekliyor.</summary>
    Open,

    /// <summary>Yeniden işlendi; tekrar kontrol bekliyor.</summary>
    Done,

    /// <summary>Tekrar kontrolden geçti.</summary>
    Approved,

    /// <summary>Tekrar kontrolde yine uygun bulunmadı (ret ya da yeni tamir).</summary>
    Rejected,
}

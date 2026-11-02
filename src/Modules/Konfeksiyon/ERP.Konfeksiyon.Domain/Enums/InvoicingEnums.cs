namespace ERP.Konfeksiyon.Domain.Enums;

public enum InvoiceStatus
{
    Draft,
    Confirmed,
    Cancelled,
}

public enum PaymentMethod
{
    Cash,
    BankTransfer,
    Check,

    /// <summary>Sisteme geçişte müşterinin alacak bakiyesi; gerçek tahsilat değildir.</summary>
    Opening,
}

public enum PaymentStatus
{
    Active,
    Cancelled,
}

namespace ERP.Core.Application.Abstractions;

/// <param name="TextBody">Düz metin gövde (HTML göremeyen istemciler için).</param>
public sealed record EmailMessage(string To, string Subject, string HtmlBody, string TextBody);

/// <summary>E-posta gönderimi (SMTP ayarları altyapı katmanındadır).</summary>
public interface IEmailSender
{
    /// <summary>Gönderim ayarları yapılmış ve açık.</summary>
    bool IsConfigured { get; }

    /// <summary>Gönderemezse <see cref="EmailSendException"/> fırlatır.</summary>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

public sealed class EmailSendException(string message, Exception? inner = null) : Exception(message, inner);

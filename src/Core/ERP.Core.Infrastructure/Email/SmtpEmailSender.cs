using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using ERP.Core.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace ERP.Core.Infrastructure.Email;

/// <summary>SMTP ayarları (appsettings: Email). Host ve gönderen adresi boşsa e-posta gönderimi kapalıdır.</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string? Host { get; set; }

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    /// <summary>STARTTLS / SSL.</summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>Kimlik doğrulama gerekmiyorsa boş bırakılır.</summary>
    public string? UserName { get; set; }
    public string? Password { get; set; }

    public string? FromAddress { get; set; }
    public string FromName { get; set; } = "Tekstil ERP";

    [Range(5, 300)]
    public int TimeoutSeconds { get; set; } = 30;
}

public sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.Host) && !string.IsNullOrWhiteSpace(_options.FromAddress);

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new EmailSendException("E-posta gönderimi yapılandırılmamış.");
        }

        using var mail = new MailMessage
        {
            From = new MailAddress(_options.FromAddress!, _options.FromName),
            Subject = message.Subject,
            Body = message.TextBody,
            IsBodyHtml = false,
        };
        mail.To.Add(message.To);
        mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(message.HtmlBody, null, MediaTypeNames.Text.Html));

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Timeout = _options.TimeoutSeconds * 1000,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };
        if (!string.IsNullOrWhiteSpace(_options.UserName))
        {
            client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
        }

        try
        {
            await client.SendMailAsync(mail, cancellationToken);
        }
        catch (Exception exception) when (exception is SmtpException or InvalidOperationException or FormatException)
        {
            throw new EmailSendException($"E-posta gönderilemedi: {exception.Message}", exception);
        }
    }
}

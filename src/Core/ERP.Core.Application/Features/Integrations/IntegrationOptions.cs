using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using ERP.Core.Domain.Entities;

namespace ERP.Core.Application.Features.Integrations;

public sealed partial class IntegrationOptions : IValidatableObject
{
    public const string SectionName = "Integrations";

    /// <summary>Kapalıysa olaylar yine yazılır ama arka planda gönderilmez.</summary>
    public bool DispatchEnabled { get; set; } = true;

    /// <summary>Gönderilecek teslim var mı diye bakma aralığı.</summary>
    [Range(1, 3600)]
    public int PollSeconds { get; set; } = 5;

    /// <summary>Bir turda işlenen en fazla teslim.</summary>
    [Range(1, 500)]
    public int BatchSize { get; set; } = 50;

    /// <summary>Bir teslim en fazla kaç kez denenir; sonra kullanıcı yeniden kuyruğa alana kadar bekler.</summary>
    [Range(1, 100)]
    public int MaxAttempts { get; set; } = 10;

    /// <summary>İlk başarısız denemeden sonraki bekleme; her denemede ikiye katlanır.</summary>
    [Range(1, 3600)]
    public int RetryBaseSeconds { get; set; } = 30;

    [Range(1, 10080)]
    public int RetryMaxMinutes { get; set; } = 360;

    /// <summary>Gönderimi süren teslimin kilit süresi; gönderici çökerse teslim bu süre sonunda yeniden alınır.</summary>
    [Range(30, 3600)]
    public int LeaseSeconds { get; set; } = 120;

    public List<WebhookOptions> Webhooks { get; set; } = [];

    public IntegrationRetryPolicy RetryPolicy =>
        new(MaxAttempts, TimeSpan.FromSeconds(RetryBaseSeconds), TimeSpan.FromMinutes(RetryMaxMinutes));

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < Webhooks.Count; i++)
        {
            var webhook = Webhooks[i];
            var path = $"{SectionName}:{nameof(Webhooks)}:{i}";

            if (!WebhookName().IsMatch(webhook.Name ?? string.Empty))
            {
                yield return new ValidationResult(
                    $"{path}:Name küçük harf, rakam ve tireden oluşmalı (en fazla 50 karakter).");
            }
            else if (!names.Add(webhook.Name!))
            {
                yield return new ValidationResult($"{path}:Name '{webhook.Name}' birden fazla kez tanımlı.");
            }

            if (!Uri.TryCreate(webhook.Url, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            {
                yield return new ValidationResult($"{path}:Url geçerli bir http(s) adresi olmalı.");
            }

            if ((webhook.Secret?.Length ?? 0) < WebhookOptions.MinSecretLength)
            {
                yield return new ValidationResult(
                    $"{path}:Secret en az {WebhookOptions.MinSecretLength} karakter olmalı (istekler bununla imzalanır).");
            }

            // Kilit, istek sürerken dolmamalı; yoksa aynı teslim ikinci kez gönderilir.
            if (webhook.TimeoutSeconds < 1 || webhook.TimeoutSeconds > LeaseSeconds - 10)
            {
                yield return new ValidationResult(
                    $"{path}:TimeoutSeconds 1 ile {LeaseSeconds - 10} (LeaseSeconds - 10) arasında olmalı.");
            }
        }
    }

    [GeneratedRegex("^[a-z0-9-]{1,50}$")]
    private static partial Regex WebhookName();
}

/// <summary>Olayların HTTP POST ile gönderildiği bir adres. Gizli anahtar yalnızca yapılandırmada tutulur.</summary>
public sealed class WebhookOptions
{
    public const int MinSecretLength = 16;

    /// <summary>Kanalın kalıcı adı; teslim kayıtlarında <c>webhook:{Name}</c> olarak saklanır.</summary>
    public string Name { get; set; } = null!;

    public string Url { get; set; } = null!;

    /// <summary>İsteklerin HMAC-SHA256 ile imzalandığı gizli anahtar; alıcıyla paylaşılır.</summary>
    public string Secret { get; set; } = null!;

    /// <summary>Kapalı webhook kanal sayılmaz: yeni olay yazılmaz, bekleyen teslimleri başarısız olur.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gönderilecek olay türleri; boşsa tüm olaylar.</summary>
    public List<string> EventTypes { get; set; } = [];

    public int TimeoutSeconds { get; set; } = 15;
}

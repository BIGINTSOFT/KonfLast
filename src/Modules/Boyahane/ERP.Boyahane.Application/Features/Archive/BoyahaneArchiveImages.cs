using FluentValidation;

namespace ERP.Boyahane.Application.Features.Archive;

/// <summary>Renk ve desen kartının görüntüsü (base64).</summary>
public sealed record BoyahaneArchiveImageDto(string ContentType, string DataBase64, DateTime UpdatedAt);

/// <param name="ContentType">image/jpeg ya da image/png.</param>
/// <param name="DataBase64">Görüntünün base64 kodlanmış içeriği (en fazla 2 MB).</param>
public sealed record BoyahaneArchiveImageRequest(string ContentType, string DataBase64);

public sealed class BoyahaneArchiveImageRequestValidator : AbstractValidator<BoyahaneArchiveImageRequest>
{
    // Base64, veriyi 4/3 oranında büyütür; çözmeden önce kaba boyut denetimi için.
    private const int MaxBase64Length = (BoyahaneArchiveImages.MaxBytes + 2) / 3 * 4;

    public BoyahaneArchiveImageRequestValidator()
    {
        RuleFor(x => x.ContentType)
            .Must(type => BoyahaneArchiveImages.Signatures.ContainsKey(type ?? string.Empty))
            .WithMessage("Görüntü JPEG ya da PNG olmalıdır.");
        RuleFor(x => x.DataBase64)
            .NotEmpty().WithMessage("Görüntü zorunludur.")
            .MaximumLength(MaxBase64Length).WithMessage($"Görüntü en fazla {BoyahaneArchiveImages.MaxBytes / 1024 / 1024} MB olabilir.");
    }
}

/// <summary>Renk ve desen kartı görüntüleri: yalnızca JPEG / PNG, en fazla 2 MB; ayrı tabloda bayt olarak saklanır.</summary>
public static class BoyahaneArchiveImages
{
    public const int MaxBytes = 2 * 1024 * 1024;

    /// <summary>Kabul edilen türler ve dosyanın başında beklenen imza baytları.</summary>
    public static readonly IReadOnlyDictionary<string, byte[]> Signatures = new Dictionary<string, byte[]>
    {
        ["image/jpeg"] = [0xFF, 0xD8, 0xFF],
        ["image/png"] = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
    };

    /// <summary>Base64 çözülemiyorsa, boyut aşılıyorsa ya da dosya imzası türüyle uyuşmuyorsa boş döner.</summary>
    public static byte[]? Decode(BoyahaneArchiveImageRequest request)
    {
        byte[] content;
        try
        {
            content = Convert.FromBase64String(request.DataBase64);
        }
        catch (FormatException)
        {
            return null;
        }

        return content.Length <= MaxBytes
            && Signatures.TryGetValue(request.ContentType, out var signature)
            && content.AsSpan().StartsWith(signature)
            ? content
            : null;
    }
}

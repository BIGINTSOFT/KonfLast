using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace ERP.Core.Application.Features.Auth;

/// <summary>Parola kuralları konfigürasyondan yönetilir (Auth:Password).</summary>
public sealed class PasswordPolicyOptions
{
    public const string SectionName = "Auth:Password";

    [Range(1, 128)]
    public int MinLength { get; set; } = 8;

    public bool RequireDigit { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireNonAlphanumeric { get; set; }
}

public static class PasswordPolicyValidation
{
    public const int MaxLength = 128;

    public static IRuleBuilderOptions<T, string> MeetsPasswordPolicy<T>(
        this IRuleBuilder<T, string> rule, PasswordPolicyOptions policy)
    {
        var options = rule
            .NotEmpty().WithMessage("Parola zorunludur.")
            .MinimumLength(policy.MinLength).WithMessage($"Parola en az {policy.MinLength} karakter olmalıdır.")
            .MaximumLength(MaxLength).WithMessage($"Parola en fazla {MaxLength} karakter olabilir.");

        if (policy.RequireDigit)
        {
            options = options.Must(p => p is not null && p.Any(char.IsDigit))
                .WithMessage("Parola en az bir rakam içermelidir.");
        }

        if (policy.RequireLowercase)
        {
            options = options.Must(p => p is not null && p.Any(char.IsLower))
                .WithMessage("Parola en az bir küçük harf içermelidir.");
        }

        if (policy.RequireUppercase)
        {
            options = options.Must(p => p is not null && p.Any(char.IsUpper))
                .WithMessage("Parola en az bir büyük harf içermelidir.");
        }

        if (policy.RequireNonAlphanumeric)
        {
            options = options.Must(p => p is not null && p.Any(c => !char.IsLetterOrDigit(c)))
                .WithMessage("Parola en az bir özel karakter içermelidir.");
        }

        return options;
    }
}

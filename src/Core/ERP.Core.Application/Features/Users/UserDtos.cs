using ERP.Core.Application.Common;
using ERP.Core.Application.Features.Auth;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace ERP.Core.Application.Features.Users;

public sealed record UserDto(
    int Id,
    string UserName,
    string FullName,
    string? Email,
    bool IsActive,
    bool IsLockedOut,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    IReadOnlyList<string> Roles);

public sealed class UserListQuery : PageRequest
{
    /// <summary>Kullanıcı adı veya ad soyad içinde aranır.</summary>
    public string? Search { get; set; }
    public bool? IsActive { get; set; }

    /// <summary>Yalnızca bu role sahip kullanıcılar.</summary>
    public string? Role { get; set; }
}

public sealed record CreateUserRequest(
    string UserName, string FullName, string? Email, string Password, IReadOnlyList<string> Roles);

public sealed record UpdateUserRequest(string FullName, string? Email, bool IsActive, IReadOnlyList<string> Roles);

public sealed record ResetPasswordRequest(string NewPassword);

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator(IOptions<PasswordPolicyOptions> passwordPolicy)
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Kullanıcı adı zorunludur.")
            .Length(3, 50).WithMessage("Kullanıcı adı 3-50 karakter olmalıdır.")
            .Matches("^[a-zA-Z0-9._-]+$")
            .WithMessage("Kullanıcı adı yalnızca harf (a-z), rakam, nokta, tire ve alt çizgi içerebilir.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Ad soyad zorunludur.")
            .MaximumLength(150).WithMessage("Ad soyad en fazla 150 karakter olabilir.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.")
            .MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Password).MeetsPasswordPolicy(passwordPolicy.Value);
        RuleFor(x => x.Roles).NotNull().WithMessage("Roller zorunludur.");
    }
}

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Ad soyad zorunludur.")
            .MaximumLength(150).WithMessage("Ad soyad en fazla 150 karakter olabilir.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.")
            .MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Roles).NotNull().WithMessage("Roller zorunludur.");
    }
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator(IOptions<PasswordPolicyOptions> passwordPolicy)
    {
        RuleFor(x => x.NewPassword).MeetsPasswordPolicy(passwordPolicy.Value);
    }
}

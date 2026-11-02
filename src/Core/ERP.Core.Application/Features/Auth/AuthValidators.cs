using FluentValidation;
using Microsoft.Extensions.Options;

namespace ERP.Core.Application.Features.Auth;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Kullanıcı adı zorunludur.")
            .MaximumLength(50).WithMessage("Kullanıcı adı en fazla 50 karakter olabilir.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Parola zorunludur.")
            .MaximumLength(128).WithMessage("Parola en fazla 128 karakter olabilir.");
    }
}

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token zorunludur.")
            .MaximumLength(512);
    }
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator(IOptions<PasswordPolicyOptions> passwordPolicy)
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Mevcut parola zorunludur.")
            .MaximumLength(PasswordPolicyValidation.MaxLength);

        RuleFor(x => x.NewPassword)
            .MeetsPasswordPolicy(passwordPolicy.Value)
            .NotEqual(x => x.CurrentPassword).WithMessage("Yeni parola mevcut paroladan farklı olmalıdır.");
    }
}

public sealed class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token zorunludur.")
            .MaximumLength(512);
    }
}

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.UserNameOrEmail)
            .NotEmpty().WithMessage("Kullanıcı adı ya da e-posta zorunludur.")
            .MaximumLength(256);
    }
}

public sealed class ResetForgottenPasswordRequestValidator : AbstractValidator<ResetForgottenPasswordRequest>
{
    public ResetForgottenPasswordRequestValidator(IOptions<PasswordPolicyOptions> passwordPolicy)
    {
        RuleFor(x => x.Token).NotEmpty().WithMessage("Bağlantı geçersiz.").MaximumLength(512);
        RuleFor(x => x.NewPassword).MeetsPasswordPolicy(passwordPolicy.Value);
    }
}

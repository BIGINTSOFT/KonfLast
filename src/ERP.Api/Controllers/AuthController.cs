using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ERP.Api.Controllers;

[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService) : ApiControllerBase
{
    /// <summary>Kullanıcı adı ve parola ile giriş; access + refresh token döner.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(
        LoginRequest request, CancellationToken cancellationToken) =>
        FromResult(await authService.LoginAsync(request, cancellationToken));

    /// <summary>Refresh token'ı yenisiyle değiştirir (rotation) ve yeni access token döner.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh(
        RefreshTokenRequest request, CancellationToken cancellationToken) =>
        FromResult(await authService.RefreshAsync(request, cancellationToken));

    /// <summary>Verilen refresh token'ı iptal eder.</summary>
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object?>>> Logout(
        LogoutRequest request, CancellationToken cancellationToken) =>
        FromResult(await authService.LogoutAsync(request, cancellationToken));

    /// <summary>
    /// "Şifremi unuttum": kullanıcı adı ya da e-postaya kayıtlı adrese süreli, tek kullanımlık sıfırlama bağlantısı
    /// gönderilir. Hesap yoksa da aynı yanıt döner. E-posta yapılandırılmamışsa EMAIL_NOT_CONFIGURED.
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<ActionResult<ApiResponse<object?>>> ForgotPassword(
        ForgotPasswordRequest request, CancellationToken cancellationToken) =>
        FromResult(await authService.ForgotPasswordAsync(request, cancellationToken));

    /// <summary>E-postadaki bağlantıyla yeni parola belirler; hesap kilidi kalkar, açık oturumlar kapanır.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<ActionResult<ApiResponse<object?>>> ResetPassword(
        ResetForgottenPasswordRequest request, CancellationToken cancellationToken) =>
        FromResult(await authService.ResetPasswordAsync(request, cancellationToken));

    /// <summary>Kullanıcının kendi parolasını değiştirir; tüm oturumları kapanır, yeniden giriş gerekir.</summary>
    [HttpPost("change-password")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<ActionResult<ApiResponse<object?>>> ChangePassword(
        ChangePasswordRequest request, CancellationToken cancellationToken) =>
        FromResult(await authService.ChangePasswordAsync(request, cancellationToken));

    /// <summary>Oturum açmış kullanıcının bilgileri, rolleri ve yetkileri.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<CurrentUserDto>>> Me(CancellationToken cancellationToken) =>
        FromResult(await authService.GetCurrentUserAsync(cancellationToken));
}

namespace ERP.Core.Application.Features.Auth;

/// <param name="RememberMe">İşaretliyse oturum kalıcıdır (tarayıcı kapansa da sürer) ve daha uzun geçerlidir.</param>
public sealed record LoginRequest(string UserName, string Password, bool RememberMe = false);

/// <param name="UserNameOrEmail">Kullanıcı adı ya da kayıtlı e-posta adresi.</param>
public sealed record ForgotPasswordRequest(string UserNameOrEmail);

/// <param name="Token">E-postadaki bağlantıda gelen tek kullanımlık anahtar.</param>
public sealed record ResetForgottenPasswordRequest(string Token, string NewPassword);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record CurrentUserDto(
    int Id,
    string UserName,
    string FullName,
    string? Email,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    CurrentUserDto User,
    bool RememberMe = false);

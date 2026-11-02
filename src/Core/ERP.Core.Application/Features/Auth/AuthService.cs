using System.Net;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERP.Core.Application.Features.Auth;

public interface IAuthService
{
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
    Task<Result> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken);
    Task<Result<CurrentUserDto>> GetCurrentUserAsync(CancellationToken cancellationToken);

    /// <summary>Oturum açmış kullanıcının kendi parolasını değiştirir; tüm oturumları kapanır.</summary>
    Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Kullanıcı adı ya da e-postaya kayıtlı adrese süreli, tek kullanımlık parola sıfırlama bağlantısı gönderir.
    /// Hesabın var olup olmadığı yanıttan anlaşılmaz.
    /// </summary>
    Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken);

    /// <summary>Bağlantıdaki anahtarla yeni parola belirler; kilit kalkar, açık oturumlar kapanır.</summary>
    Task<Result> ResetPasswordAsync(ResetForgottenPasswordRequest request, CancellationToken cancellationToken);
}

public sealed class AuthService(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IPermissionService permissionService,
    ICurrentUser currentUser,
    IEmailSender emailSender,
    TimeProvider timeProvider,
    IOptions<AuthOptions> options,
    ILogger<AuthService> logger) : IAuthService
{
    private static readonly Error InvalidCredentials = new(
        ErrorCodes.InvalidCredentials, "Kullanıcı adı veya parola hatalı.", ErrorType.Unauthorized);

    private static readonly Error AccountLocked = new(
        ErrorCodes.AccountLocked,
        "Çok fazla hatalı giriş denemesi nedeniyle hesap geçici olarak kilitlendi.",
        ErrorType.Unauthorized);

    private static readonly Error AccountInactive = new(
        ErrorCodes.AccountInactive, "Kullanıcı hesabı pasif durumda.", ErrorType.Unauthorized);

    private static readonly Error InvalidRefreshToken = new(
        ErrorCodes.InvalidRefreshToken, "Oturum süresi doldu, lütfen tekrar giriş yapın.", ErrorType.Unauthorized);

    private readonly AuthOptions _options = options.Value;

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var normalizedUserName = User.NormalizeUserName(request.UserName);

        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken);

        if (user is null)
        {
            // Kullanıcının var olup olmadığı yanıt süresinden anlaşılmasın.
            passwordHasher.Hash(request.Password);
            logger.LogWarning("Bilinmeyen kullanıcı ile giriş denemesi: {UserName}", request.UserName);
            return InvalidCredentials;
        }

        if (user.IsLockedOut(now))
        {
            logger.LogWarning("Kilitli hesaba giriş denemesi: {UserId}", user.Id);
            return AccountLocked;
        }

        if (!passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            var lockedNow = user.RegisterFailedLogin(
                now, _options.MaxFailedLoginAttempts, TimeSpan.FromMinutes(_options.LockoutMinutes));
            await db.SaveChangesAsync(cancellationToken);

            logger.LogWarning("Hatalı parola: {UserId}, kilitlendi: {Locked}", user.Id, lockedNow);
            return lockedNow ? AccountLocked : InvalidCredentials;
        }

        if (!user.IsActive)
        {
            return AccountInactive;
        }

        user.RegisterSuccessfulLogin(now);
        var response = await IssueTokensAsync(user, now, cancellationToken, persistent: request.RememberMe);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Kullanıcı giriş yaptı: {UserId}", user.Id);
        return response;
    }

    public async Task<Result<AuthResponse>> RefreshAsync(
        RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);

        var token = await db.RefreshTokens
            .Include(t => t.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (token is null)
        {
            return InvalidRefreshToken;
        }

        if (token.IsRevoked)
        {
            // İptal edilmiş token'ın yeniden kullanılması çalınmış olabileceğine işaret eder:
            // kullanıcının tüm açık oturumları kapatılır. Tek bir UPDATE ile yapılır ki
            // eşzamanlı isteklerde satır bazlı concurrency çakışması oluşmasın.
            var closedSessions = await db.RefreshTokens
                .Where(t => t.UserId == token.UserId && t.RevokedAt == null && t.ExpiresAt > now)
                .ExecuteUpdateAsync(
                    update => update
                        .SetProperty(t => t.RevokedAt, now)
                        .SetProperty(t => t.RevokedReason, RefreshTokenRevokeReasons.ReuseDetected),
                    cancellationToken);

            logger.LogWarning(
                "İptal edilmiş refresh token yeniden kullanıldı: {UserId}, kapatılan oturum: {Count}",
                token.UserId, closedSessions);
            return InvalidRefreshToken;
        }

        if (token.IsExpired(now))
        {
            return InvalidRefreshToken;
        }

        if (!token.User.IsActive)
        {
            return AccountInactive;
        }

        var response = await IssueTokensAsync(token.User, now, cancellationToken, rotatedFrom: token, persistent: token.IsPersistent);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Aynı token eşzamanlı olarak başka bir istekle döndürüldü; yalnızca biri kazanır.
            return InvalidRefreshToken;
        }

        return response;
    }

    public async Task<Result> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);

        var token = await db.RefreshTokens
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash && t.UserId == currentUser.UserId, cancellationToken);

        if (token is not null && !token.IsRevoked)
        {
            token.Revoke(now, RefreshTokenRevokeReasons.Logout);
            await db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result<CurrentUserDto>> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(u => u.Id == currentUser.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return new Error(ErrorCodes.Unauthorized, "Oturum geçersiz.", ErrorType.Unauthorized);
        }

        return await ToCurrentUserDtoAsync(user, cancellationToken);
    }

    public async Task<Result> ChangePasswordAsync(
        ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == currentUser.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return new Error(ErrorCodes.Unauthorized, "Oturum geçersiz.", ErrorType.Unauthorized);
        }

        if (!passwordHasher.Verify(user.PasswordHash, request.CurrentPassword))
        {
            logger.LogWarning("Parola değiştirmede mevcut parola hatalı: {UserId}", user.Id);
            return new Error(
                ErrorCodes.InvalidCurrentPassword,
                "Mevcut parola hatalı.",
                ErrorType.Validation,
                new Dictionary<string, string[]> { ["currentPassword"] = ["Mevcut parola hatalı."] });
        }

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await db.RevokeUserSessionsAsync(
            user.Id, RefreshTokenRevokeReasons.PasswordChanged, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Kullanıcı parolasını değiştirdi: {UserId}", user.Id);
        return Result.Success();
    }

    public async Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        if (!emailSender.IsConfigured)
        {
            return new Error(
                ErrorCodes.EmailNotConfigured,
                "E-posta gönderimi yapılandırılmamış; parolanızı sistem yöneticiniz sıfırlayabilir.",
                ErrorType.BusinessRule);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var value = request.UserNameOrEmail.Trim();
        var normalized = User.NormalizeUserName(value);
        var email = value.ToUpperInvariant();
        var user = await db.Users.FirstOrDefaultAsync(
            u => u.NormalizedUserName == normalized || (u.Email != null && u.Email.ToUpper() == email), cancellationToken);
        if (user is null || !user.IsActive || string.IsNullOrWhiteSpace(user.Email))
        {
            // Yanıt aynıdır: hesabın varlığı ya da e-postası olup olmadığı dışarıdan anlaşılmaz.
            logger.LogWarning("Parola sıfırlama isteği karşılanmadı (kullanıcı yok, pasif ya da e-postasız): {Value}", value);
            return Result.Success();
        }

        // Önceki kullanılmamış bağlantılar geçersiz kılınır: yalnızca son gönderilen çalışır.
        await db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(t => t.UsedAt, now), cancellationToken);

        var token = tokenService.CreateRefreshToken();
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = token.TokenHash,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(_options.PasswordResetMinutes),
            RequestedByIp = currentUser.IpAddress,
        });
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await emailSender.SendAsync(ResetMail(user, token.Token), cancellationToken);
        }
        catch (EmailSendException exception)
        {
            logger.LogError(exception, "Parola sıfırlama e-postası gönderilemedi: {UserId}", user.Id);
            return new Error(
                ErrorCodes.EmailSendFailed,
                "E-posta gönderilemedi. Lütfen daha sonra tekrar deneyin ya da yöneticinize başvurun.",
                ErrorType.BusinessRule);
        }

        logger.LogInformation("Parola sıfırlama bağlantısı gönderildi: {UserId}", user.Id);
        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetForgottenPasswordRequest request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var hash = tokenService.HashRefreshToken(request.Token);
        var reset = await db.PasswordResetTokens.Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (reset is null || !reset.IsUsable(now) || !reset.User.IsActive)
        {
            return new Error(
                ErrorCodes.InvalidResetToken,
                "Bağlantı geçersiz ya da süresi dolmuş. Giriş ekranından yeni bir sıfırlama bağlantısı isteyin.",
                ErrorType.BusinessRule);
        }

        reset.UsedAt = now;
        reset.User.PasswordHash = passwordHasher.Hash(request.NewPassword);
        reset.User.ClearLockout();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await db.RevokeUserSessionsAsync(reset.UserId, RefreshTokenRevokeReasons.PasswordReset, now, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Parola e-posta bağlantısıyla sıfırlandı: {UserId}", reset.UserId);
        return Result.Success();
    }

    private EmailMessage ResetMail(User user, string token)
    {
        var link = $"{_options.WebBaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}";
        var minutes = _options.PasswordResetMinutes;
        var name = WebUtility.HtmlEncode(user.FullName);
        var userName = WebUtility.HtmlEncode(user.UserName);
        return new EmailMessage(
            user.Email!,
            "Tekstil ERP · Parola sıfırlama",
            $"<p>Merhaba {name},</p>"
            + $"<p><strong>{userName}</strong> kullanıcısı için parola sıfırlama istendi. Yeni parolanızı belirlemek için "
            + $"aşağıdaki bağlantıyı {minutes} dakika içinde açın:</p>"
            + $"<p><a href=\"{WebUtility.HtmlEncode(link)}\">Parolamı sıfırla</a></p>"
            + "<p>Bu isteği siz yapmadıysanız bu e-postayı dikkate almayın; parolanız değişmez.</p>",
            $"Merhaba {user.FullName},\n\n{user.UserName} kullanıcısı için parola sıfırlama istendi. Yeni parolanızı "
            + $"belirlemek için bu bağlantıyı {minutes} dakika içinde açın:\n{link}\n\n"
            + "Bu isteği siz yapmadıysanız bu e-postayı dikkate almayın; parolanız değişmez.");
    }

    private async Task<AuthResponse> IssueTokensAsync(
        User user, DateTime now, CancellationToken cancellationToken, RefreshToken? rotatedFrom = null, bool persistent = false)
    {
        var userDto = await ToCurrentUserDtoAsync(user, cancellationToken);
        var accessToken = tokenService.CreateAccessToken(user, userDto.Roles);
        // Kalıcı (beni hatırla) oturum gün, tarayıcı oturumu saat ölçeğinde geçerlidir.
        var refreshToken = tokenService.CreateRefreshToken() with
        {
            ExpiresAt = persistent ? now.AddDays(_options.RememberMeDays) : now.AddHours(_options.SessionHours),
        };

        rotatedFrom?.Revoke(now, RefreshTokenRevokeReasons.Rotated, refreshToken.TokenHash);

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshToken.TokenHash,
            CreatedAt = now,
            ExpiresAt = refreshToken.ExpiresAt,
            CreatedByIp = currentUser.IpAddress,
            DeviceInfo = currentUser.DeviceInfo,
            IsPersistent = persistent,
        });

        return new AuthResponse(
            accessToken.Token, accessToken.ExpiresAt, refreshToken.Token, refreshToken.ExpiresAt, userDto, persistent);
    }

    private async Task<CurrentUserDto> ToCurrentUserDtoAsync(User user, CancellationToken cancellationToken)
    {
        var roles = user.UserRoles.Select(ur => ur.Role.Code).Order().ToList();
        var permissions = await permissionService.GetPermissionsAsync(roles, cancellationToken);

        return new CurrentUserDto(
            user.Id, user.UserName, user.FullName, user.Email, roles, permissions.Order().ToList());
    }
}

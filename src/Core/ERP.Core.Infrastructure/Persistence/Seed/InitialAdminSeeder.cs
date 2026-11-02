using ERP.Core.Application.Abstractions;
using ERP.Core.Domain.Authorization;
using ERP.Core.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERP.Core.Infrastructure.Persistence.Seed;

public sealed class InitialAdminOptions
{
    public const string SectionName = "InitialAdmin";

    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string FullName { get; set; } = "Sistem Yöneticisi";
}

/// <summary>
/// Veritabanında hiç kullanıcı yoksa konfigürasyondaki bilgilerle ilk ADMIN kullanıcısını oluşturur.
/// Parola script'e gömülmez; kurulumda konfigürasyondan verilir ve hash'lenerek saklanır.
/// </summary>
public sealed class InitialAdminSeeder(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    IOptions<InitialAdminOptions> options,
    ILogger<InitialAdminSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.UserName) || string.IsNullOrWhiteSpace(settings.Password))
        {
            return;
        }

        if (await db.Users.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            return;
        }

        var adminRoleId = await db.Roles
            .Where(r => r.Code == Roles.Admin)
            .Select(r => r.Id)
            .SingleAsync(cancellationToken);

        var user = new User
        {
            UserName = settings.UserName.Trim(),
            NormalizedUserName = User.NormalizeUserName(settings.UserName),
            FullName = settings.FullName,
            PasswordHash = passwordHasher.Hash(settings.Password),
            IsActive = true,
        };
        user.UserRoles.Add(new UserRole { RoleId = adminRoleId });

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("İlk yönetici kullanıcısı oluşturuldu: {UserName}", user.UserName);
    }
}

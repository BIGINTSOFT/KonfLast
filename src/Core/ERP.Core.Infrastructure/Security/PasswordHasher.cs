using ERP.Core.Application.Abstractions;
using ERP.Core.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace ERP.Core.Infrastructure.Security;

/// <summary>ASP.NET Core Identity'nin PBKDF2 (HMAC-SHA512, rastgele salt) hash algoritmasını kullanır.</summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(null!, passwordHash, password) != PasswordVerificationResult.Failed;
}

using ERP.Core.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Core.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.Property(u => u.UserName).HasMaxLength(50).IsRequired();
        builder.Property(u => u.NormalizedUserName).HasMaxLength(50).IsRequired();
        builder.Property(u => u.FullName).HasMaxLength(150).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(256);
        builder.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();

        builder.HasIndex(u => u.NormalizedUserName).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.Property(r => r.Code).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.Property(r => r.ModuleKey).HasMaxLength(50);
        builder.HasIndex(r => r.Code).IsUnique();
    }
}

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");

        builder.Property(p => p.Code).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Module).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(250).IsRequired();
        builder.HasIndex(p => p.Code).IsUnique();
    }
}

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });

        builder.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId);
        builder.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId);
        builder.HasIndex(ur => ur.RoleId);

        // Silinmiş kullanıcının satırları da sorgulara dahil olmasın (User filtresiyle uyumlu).
        builder.HasQueryFilter(ur => !ur.User.IsDeleted);
    }
}

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        builder.HasOne(rp => rp.Role).WithMany(r => r.RolePermissions).HasForeignKey(rp => rp.RoleId);
        builder.HasOne(rp => rp.Permission).WithMany(p => p.RolePermissions).HasForeignKey(rp => rp.PermissionId);
        builder.HasIndex(rp => rp.PermissionId);
    }
}

public sealed class UserShortcutConfiguration : IEntityTypeConfiguration<UserShortcut>
{
    public void Configure(EntityTypeBuilder<UserShortcut> builder)
    {
        builder.ToTable("UserShortcuts");

        builder.Property(s => s.Path).HasMaxLength(UserShortcut.PathMaxLength).IsRequired();

        builder.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId);

        builder.HasIndex(s => new { s.UserId, s.Path }).IsUnique();
        builder.HasIndex(s => new { s.UserId, s.SortOrder });
    }
}

public sealed class UserFormDraftConfiguration : IEntityTypeConfiguration<UserFormDraft>
{
    public void Configure(EntityTypeBuilder<UserFormDraft> builder)
    {
        builder.ToTable("UserFormDrafts");

        builder.Property(d => d.FormKey).HasMaxLength(UserFormDraft.KeyMaxLength).IsUnicode(false).IsRequired();
        builder.Property(d => d.Payload).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(d => d.Title).HasMaxLength(200);

        builder.HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserId);

        builder.HasIndex(d => new { d.UserId, d.FormKey }).IsUnique();
    }
}

public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");

        builder.Property(t => t.TokenHash).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(t => t.RequestedByIp).HasMaxLength(45);

        builder.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId);

        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.UserId);
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.Property(t => t.TokenHash).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(64).IsUnicode(false);
        builder.Property(t => t.CreatedByIp).HasMaxLength(45);
        builder.Property(t => t.DeviceInfo).HasMaxLength(300);
        builder.Property(t => t.RevokedReason).HasMaxLength(30);
        builder.Property(t => t.RowVersion).IsRowVersion();

        builder.HasOne(t => t.User).WithMany(u => u.RefreshTokens).HasForeignKey(t => t.UserId);

        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => new { t.UserId, t.ExpiresAt });

        builder.HasQueryFilter(t => !t.User.IsDeleted);
    }
}

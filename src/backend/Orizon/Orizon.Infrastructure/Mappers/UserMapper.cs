using Riok.Mapperly.Abstractions;
using Orizon.Domain.Entities;
using Orizon.Infrastructure.Identity;

namespace Orizon.Infrastructure.Mappers;

[Mapper]
public partial class UserMapper
{
    // AppIdentityUser → AppUser (Domain)
    [MapProperty(nameof(AppIdentityUser.Id), nameof(AppUser.Id))]
    [MapperIgnoreSource(nameof(AppIdentityUser.GoogleAccessToken))]
    [MapperIgnoreSource(nameof(AppIdentityUser.GoogleRefreshToken))]
    [MapperIgnoreSource(nameof(AppIdentityUser.GoogleTokenExpiry))]
    [MapperIgnoreSource(nameof(AppIdentityUser.TrelloApiKey))]
    [MapperIgnoreSource(nameof(AppIdentityUser.TrelloToken))]
    [MapperIgnoreSource(nameof(AppIdentityUser.UserName))]
    [MapperIgnoreSource(nameof(AppIdentityUser.NormalizedUserName))]
    [MapperIgnoreSource(nameof(AppIdentityUser.NormalizedEmail))]
    [MapperIgnoreSource(nameof(AppIdentityUser.EmailConfirmed))]
    [MapperIgnoreSource(nameof(AppIdentityUser.PasswordHash))]
    [MapperIgnoreSource(nameof(AppIdentityUser.SecurityStamp))]
    [MapperIgnoreSource(nameof(AppIdentityUser.ConcurrencyStamp))]
    [MapperIgnoreSource(nameof(AppIdentityUser.PhoneNumber))]
    [MapperIgnoreSource(nameof(AppIdentityUser.PhoneNumberConfirmed))]
    [MapperIgnoreSource(nameof(AppIdentityUser.TwoFactorEnabled))]
    [MapperIgnoreSource(nameof(AppIdentityUser.LockoutEnd))]
    [MapperIgnoreSource(nameof(AppIdentityUser.LockoutEnabled))]
    [MapperIgnoreSource(nameof(AppIdentityUser.AccessFailedCount))]
    // Campos do Domain que não existem no Identity
    [MapperIgnoreTarget(nameof(AppUser.BriefingEntries))]
    [MapperIgnoreTarget(nameof(AppUser.TrelloBoardConfigs))]
    [MapperIgnoreTarget(nameof(AppUser.CreatedAt))]
    [MapperIgnoreTarget(nameof(AppUser.UpdatedAt))]
    public partial AppUser ToAppUser(AppIdentityUser source);

    // AppUser (Domain) → AppIdentityUser
    [MapProperty(nameof(AppUser.Id), nameof(AppIdentityUser.Id))]
    [MapperIgnoreTarget(nameof(AppIdentityUser.GoogleAccessToken))]
    [MapperIgnoreTarget(nameof(AppIdentityUser.GoogleRefreshToken))]
    [MapperIgnoreTarget(nameof(AppIdentityUser.GoogleTokenExpiry))]
    [MapperIgnoreTarget(nameof(AppIdentityUser.TrelloApiKey))]
    [MapperIgnoreTarget(nameof(AppIdentityUser.TrelloToken))]
    [MapperIgnoreTarget(nameof(AppIdentityUser.UserName))]
    [MapperIgnoreTarget(nameof(AppIdentityUser.NormalizedUserName))]
    [MapperIgnoreTarget(nameof(AppIdentityUser.NormalizedEmail))]
    [MapperIgnoreTarget(nameof(AppIdentityUser.EmailConfirmed))]
    [MapperIgnoreTarget(nameof(AppIdentityUser.PasswordHash))]
    [MapperIgnoreTarget(nameof(AppIdentityUser.SecurityStamp))]
    [MapperIgnoreTarget(nameof(AppIdentityUser.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(AppIdentityUser.PhoneNumber))]
    [MapperIgnoreTarget(nameof(AppIdentityUser.PhoneNumberConfirmed))]
    [MapperIgnoreTarget(nameof(AppIdentityUser.TwoFactorEnabled))]
    [MapperIgnoreTarget(nameof(AppIdentityUser.LockoutEnd))]
    [MapperIgnoreTarget(nameof(AppIdentityUser.LockoutEnabled))]
    [MapperIgnoreTarget(nameof(AppIdentityUser.AccessFailedCount))]
    [MapperIgnoreSource(nameof(AppUser.BriefingEntries))]
    [MapperIgnoreSource(nameof(AppUser.TrelloBoardConfigs))]
    [MapperIgnoreSource(nameof(AppUser.CreatedAt))]
    [MapperIgnoreSource(nameof(AppUser.UpdatedAt))]
    public partial AppIdentityUser ToIdentityUser(AppUser source);
}
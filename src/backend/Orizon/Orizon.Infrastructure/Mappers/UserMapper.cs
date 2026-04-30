using Riok.Mapperly.Abstractions;
using Orizon.Domain.Entities;
using Orizon.Domain.Enums;
using Orizon.Infrastructure.Identity;

namespace Orizon.Infrastructure.Mappers;

[Mapper]
public partial class UserMapper
{    
    [MapProperty(nameof(AppIdentityUser.Id), nameof(AppUser.Id))]
    public partial AppUser ToAppUser(AppIdentityUser source);
    
    [MapProperty(nameof(AppUser.Id), nameof(AppIdentityUser.Id))]
    public partial AppIdentityUser ToIdentityUser(AppUser source);
}
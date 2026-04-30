using Microsoft.EntityFrameworkCore;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Domain.Entities;
using Orizon.Infrastructure.Data;
using Orizon.Infrastructure.Mappers;

namespace Orizon.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly OrizonDbContext _context;
    private readonly UserMapper _mapper;

    public UserRepository(OrizonDbContext context)
    {
        _context = context;
        _mapper = new UserMapper();
    }

    public async Task<IEnumerable<AppUser>> GetActiveUsersAsync(
        CancellationToken ct = default)
    {
        var identityUsers = await _context.Users
            .Where(u =>
                u.GoogleAccessToken != null &&
                u.GoogleRefreshToken != null)
            .ToListAsync(ct);

        // Mapeamento limpo com Mapperly
        return identityUsers.Select(_mapper.ToAppUser);
    }

    public async Task<AppUser?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var identityUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id.ToString(), ct);

        return identityUser is null
            ? null
            : _mapper.ToAppUser(identityUser);
    }

    public async Task UpdateAsync(
        AppUser user,
        CancellationToken ct = default)
    {
        var identityUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == user.Id.ToString(), ct);

        if (identityUser is null) return;

        // Mapperly mapeia os campos automaticamente
        var updated = _mapper.ToIdentityUser(user);
        updated.Id = identityUser.Id;
        updated.PasswordHash = identityUser.PasswordHash;
        updated.SecurityStamp = identityUser.SecurityStamp;

        _context.Entry(identityUser).CurrentValues
            .SetValues(updated);

        await _context.SaveChangesAsync(ct);
    }
}
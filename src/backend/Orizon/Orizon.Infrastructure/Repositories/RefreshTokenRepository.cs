using Microsoft.EntityFrameworkCore;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Domain.Entities;
using Orizon.Infrastructure.Data;

namespace Orizon.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly OrizonDbContext _context;

    public RefreshTokenRepository(OrizonDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        RefreshToken refreshToken,
        CancellationToken ct = default)
    {
        await _context.RefreshTokens.AddAsync(refreshToken, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<RefreshToken?> GetByTokenAsync(
        string token,
        CancellationToken ct = default)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token, ct);
    }

    public async Task RevokeAllUserTokensAsync(
        string userId,
        CancellationToken ct = default)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
            token.Revoke();

        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteExpiredTokensAsync(
        CancellationToken ct = default)
    {
        var expiredTokens = await _context.RefreshTokens
            .Where(t => t.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(ct);

        _context.RefreshTokens.RemoveRange(expiredTokens);
        await _context.SaveChangesAsync(ct);
    }
}
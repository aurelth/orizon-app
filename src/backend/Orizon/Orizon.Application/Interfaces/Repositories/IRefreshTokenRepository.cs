using Orizon.Domain.Entities;

namespace Orizon.Application.Interfaces.Repositories;

public interface IRefreshTokenRepository
{    
    Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default);
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);    
    Task RevokeAllUserTokensAsync(string userId, CancellationToken ct = default);    
    Task DeleteExpiredTokensAsync(CancellationToken ct = default);
}
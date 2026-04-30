using Orizon.Domain.Entities;

namespace Orizon.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<IEnumerable<AppUser>> GetActiveUsersAsync(CancellationToken ct = default);
    Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(AppUser user, CancellationToken ct = default);
}
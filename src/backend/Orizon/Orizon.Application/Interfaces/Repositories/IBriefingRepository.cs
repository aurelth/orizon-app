using Orizon.Domain.Entities;

namespace Orizon.Application.Interfaces.Repositories;

public interface IBriefingRepository
{
    Task<BriefingEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<BriefingEntry?> GetByUserAndDateAsync(string userId, DateOnly date, CancellationToken ct = default);
    Task<IEnumerable<BriefingEntry>> GetByUserAsync(string userId, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task AddAsync(BriefingEntry briefing, CancellationToken ct = default);
    Task UpdateAsync(BriefingEntry briefing, CancellationToken ct = default);
}
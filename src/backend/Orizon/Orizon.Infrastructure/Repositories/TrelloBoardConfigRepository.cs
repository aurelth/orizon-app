using Microsoft.EntityFrameworkCore;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Domain.Entities;
using Orizon.Infrastructure.Data;

namespace Orizon.Infrastructure.Repositories;

public class TrelloBoardConfigRepository : ITrelloBoardConfigRepository
{
    private readonly OrizonDbContext _context;

    public TrelloBoardConfigRepository(OrizonDbContext context)
    {
        _context = context;
    }

    public async Task<TrelloBoardConfig?> GetByUserAndBoardAsync(
        Guid userId,
        string boardId,
        CancellationToken ct = default)
    {
        return await _context.TrelloBoardConfigs
            .FirstOrDefaultAsync(
                t => t.UserId == userId && t.BoardId == boardId, ct);
    }

    public async Task<IEnumerable<TrelloBoardConfig>> GetByUserAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        return await _context.TrelloBoardConfigs
            .Where(t => t.UserId == userId && t.IsActive)
            .ToListAsync(ct);
    }

    public async Task AddAsync(
        TrelloBoardConfig config,
        CancellationToken ct = default)
    {
        await _context.TrelloBoardConfigs.AddAsync(config, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(
        TrelloBoardConfig config,
        CancellationToken ct = default)
    {
        _context.TrelloBoardConfigs.Update(config);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var config = await _context.TrelloBoardConfigs
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (config != null)
        {
            _context.TrelloBoardConfigs.Remove(config);
            await _context.SaveChangesAsync(ct);
        }
    }
}
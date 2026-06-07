using Microsoft.EntityFrameworkCore;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Domain.Entities;
using Orizon.Domain.Enums;
using Orizon.Infrastructure.Data;

namespace Orizon.Infrastructure.Repositories;

public class BriefingRepository : IBriefingRepository
{
    private readonly OrizonDbContext _context;

    public BriefingRepository(OrizonDbContext context)
    {
        _context = context;
    }

    public async Task<BriefingEntry?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        return await _context.BriefingEntries
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    public async Task<BriefingEntry?> GetByUserAndDateAsync(
        string userId,
        DateOnly date,
        CancellationToken ct = default)
    {
        return await _context.BriefingEntries
            .FirstOrDefaultAsync(
                b => b.UserId.ToString() == userId && b.Date == date,
                ct);
    }

    public async Task<(IEnumerable<BriefingEntry> Items, int Total)> GetByUserAsync(
        string userId,
        int page = 1,
        int pageSize = 10,
        DateOnly? dateFrom = null,
        DateOnly? dateTo = null,
        CancellationToken ct = default)
    {
        var query = _context.BriefingEntries
            .Where(b => b.UserId.ToString() == userId);
        
        if (dateFrom.HasValue)
            query = query.Where(b => b.Date >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(b => b.Date <= dateTo.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(b => b.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IEnumerable<DateOnly>> GetGeneratedDatesByUserAsync(
        string userId,
        CancellationToken ct = default)
    {        
        return await _context.BriefingEntries
            .Where(b => b.UserId.ToString() == userId && b.Status == BriefingStatus.Generated)
            .Select(b => b.Date)
            .ToListAsync(ct);
    }

    public async Task AddAsync(
        BriefingEntry briefing,
        CancellationToken ct = default)
    {
        await _context.BriefingEntries.AddAsync(briefing, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(
        BriefingEntry briefing,
        CancellationToken ct = default)
    {
        briefing.UpdatedAt = DateTime.UtcNow;
        _context.BriefingEntries.Update(briefing);
        await _context.SaveChangesAsync(ct);
    }
}
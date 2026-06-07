using MediatR;
using Orizon.Application.DTOs.User;
using Orizon.Application.Interfaces.Repositories;

namespace Orizon.Application.UseCases.Users.Queries.GetUserStats;

public class GetUserStatsQueryHandler
    : IRequestHandler<GetUserStatsQuery, UserStatsDto>
{
    private readonly IBriefingRepository _briefingRepository;

    public GetUserStatsQueryHandler(IBriefingRepository briefingRepository)
    {
        _briefingRepository = briefingRepository;
    }

    public async Task<UserStatsDto> Handle(
        GetUserStatsQuery request,
        CancellationToken cancellationToken)
    {
        var dates = (await _briefingRepository.GetGeneratedDatesByUserAsync(
            request.UserId, cancellationToken))
            .ToList();

        var totalGenerated = dates.Count;
        
        var brasiliaZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brasiliaZone));
        var dateSet = new HashSet<DateOnly>(dates);
        
        var currentStreak = 0;
        var checkDate = today;
        while (dateSet.Contains(checkDate))
        {
            currentStreak++;
            checkDate = checkDate.AddDays(-1);
        }
        
        var sortedDates = dates.OrderBy(d => d).ToList();
        var maxStreak = 0;
        var tempStreak = 0;
        DateOnly? prevDate = null;

        foreach (var date in sortedDates)
        {
            if (prevDate is null || date == prevDate.Value.AddDays(1))
            {
                tempStreak++;
                maxStreak = Math.Max(maxStreak, tempStreak);
            }
            else
            {
                tempStreak = 1;
            }
            prevDate = date;
        }

        return new UserStatsDto
        {
            TotalGenerated = totalGenerated,
            CurrentStreak = currentStreak,
            MaxStreak = Math.Max(maxStreak, currentStreak)
        };
    }
}
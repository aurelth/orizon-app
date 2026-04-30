using Orizon.Application.DTOs.Calendar;

namespace Orizon.Application.Interfaces.Services;

public interface ICalendarService
{    
    Task<IEnumerable<CalendarEventDto>> GetTodayEventsAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
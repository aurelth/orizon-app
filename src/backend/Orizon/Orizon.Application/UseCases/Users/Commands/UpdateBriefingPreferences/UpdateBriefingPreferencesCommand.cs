using MediatR;

namespace Orizon.Application.UseCases.Users.Commands.UpdateBriefingPreferences;

public record UpdateBriefingPreferencesCommand(
    Guid UserId,
    int BriefingHour,
    bool EmailSectionEnabled,
    bool CalendarSectionEnabled,
    bool TrelloSectionEnabled,
    bool TasksSectionEnabled,
    bool WeatherSectionEnabled
) : IRequest;
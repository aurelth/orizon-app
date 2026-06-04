namespace Orizon.API.Requests.Users;

public record UpdateBriefingPreferencesRequest(
    int BriefingHour,
    bool EmailSectionEnabled,
    bool CalendarSectionEnabled,
    bool TrelloSectionEnabled,
    bool TasksSectionEnabled,
    bool WeatherSectionEnabled
);
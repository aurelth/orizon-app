namespace Orizon.Application.DTOs.Claude;

public record BriefingSummaryDto(
    string WeatherSummary,
    string EmailsSummary,
    string CalendarSummary,
    string TrelloSummary,
    List<string> AiSuggestions
);
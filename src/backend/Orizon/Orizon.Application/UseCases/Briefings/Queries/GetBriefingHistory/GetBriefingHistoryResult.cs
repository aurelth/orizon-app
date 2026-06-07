namespace Orizon.Application.UseCases.Briefings.Queries.GetBriefingHistory;

public record GetBriefingHistoryResult(
    IEnumerable<BriefingHistoryItemDto> Items,
    int Page,
    int PageSize,
    int Total,
    int TotalPages);

public record BriefingHistoryItemDto(
    Guid BriefingId,
    DateOnly Date,
    string Status,
    string? Greeting,
    string? WeatherEmoji,
    DateTime? GeneratedAt);
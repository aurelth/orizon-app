namespace Orizon.Application.UseCases.Briefings.Queries.GetBriefingHistory;

public record GetBriefingHistoryResult(
    IEnumerable<BriefingHistoryItemDto> Items,
    int Page,
    int PageSize);

public record BriefingHistoryItemDto(
    Guid BriefingId,
    DateOnly Date,
    string Status,
    DateTime? GeneratedAt);
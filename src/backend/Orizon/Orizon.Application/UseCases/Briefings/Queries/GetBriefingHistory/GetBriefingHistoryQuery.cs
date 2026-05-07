using MediatR;

namespace Orizon.Application.UseCases.Briefings.Queries.GetBriefingHistory;

public record GetBriefingHistoryQuery(
    string UserId,
    int Page = 1,
    int PageSize = 10) : IRequest<GetBriefingHistoryResult>;
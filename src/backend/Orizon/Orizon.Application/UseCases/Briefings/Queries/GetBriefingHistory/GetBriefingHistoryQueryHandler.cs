using MediatR;
using Orizon.Application.Interfaces.Repositories;

namespace Orizon.Application.UseCases.Briefings.Queries.GetBriefingHistory;

public class GetBriefingHistoryQueryHandler
    : IRequestHandler<GetBriefingHistoryQuery, GetBriefingHistoryResult>
{
    private readonly IBriefingRepository _briefingRepository;

    public GetBriefingHistoryQueryHandler(IBriefingRepository briefingRepository)
    {
        _briefingRepository = briefingRepository;
    }

    public async Task<GetBriefingHistoryResult> Handle(
        GetBriefingHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var briefings = await _briefingRepository.GetByUserAsync(
            request.UserId,
            request.Page,
            request.PageSize,
            cancellationToken);

        var items = briefings.Select(b => new BriefingHistoryItemDto(
            b.Id,
            b.Date,
            b.Status.ToString(),
            b.GeneratedAt));

        return new GetBriefingHistoryResult(items, request.Page, request.PageSize);
    }
}
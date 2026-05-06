using MediatR;
using Orizon.Application.DTOs.Briefing;

namespace Orizon.Application.UseCases.Briefings.Queries.GetBriefingByDate;

public record GetBriefingByDateQuery(
    string UserId,
    string UserName,
    DateOnly Date) : IRequest<BriefingResultDto?>;
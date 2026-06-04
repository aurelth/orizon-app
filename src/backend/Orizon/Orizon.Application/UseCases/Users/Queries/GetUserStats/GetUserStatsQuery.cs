using MediatR;
using Orizon.Application.DTOs.User;

namespace Orizon.Application.UseCases.Users.Queries.GetUserStats;

public record GetUserStatsQuery(string UserId) : IRequest<UserStatsDto>;
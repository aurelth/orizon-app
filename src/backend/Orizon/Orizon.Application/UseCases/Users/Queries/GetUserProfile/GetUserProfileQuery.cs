using MediatR;
using Orizon.Application.DTOs.User;

namespace Orizon.Application.UseCases.Users.Queries.GetUserProfile;

public record GetUserProfileQuery(Guid UserId) : IRequest<UserProfileDto?>;
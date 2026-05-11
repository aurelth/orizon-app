using MediatR;

namespace Orizon.Application.UseCases.Users.Commands.UpdateUserProfile;

public record UpdateUserProfileCommand(
    Guid UserId,
    string DisplayName,
    string? ProfilePictureUrl,
    string? ThemePreference
) : IRequest;
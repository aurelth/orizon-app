using MediatR;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Domain.Enums;

namespace Orizon.Application.UseCases.Users.Commands.UpdateUserProfile;

public class UpdateUserProfileCommandHandler
    : IRequestHandler<UpdateUserProfileCommand>
{
    private readonly IUserRepository _userRepository;

    public UpdateUserProfileCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task Handle(
        UpdateUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null) return;

        user.DisplayName = request.DisplayName;
        user.ProfilePictureUrl = request.ProfilePictureUrl;

        if (request.ThemePreference != null &&
            Enum.TryParse<ThemePreference>(request.ThemePreference, out var theme))
        {
            user.ThemePreference = theme;
        }

        await _userRepository.UpdateAsync(user, cancellationToken);
    }
}
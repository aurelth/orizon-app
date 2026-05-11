using MediatR;
using Orizon.Application.DTOs.User;
using Orizon.Application.Interfaces.Repositories;

namespace Orizon.Application.UseCases.Users.Queries.GetUserProfile;

public class GetUserProfileQueryHandler
    : IRequestHandler<GetUserProfileQuery, UserProfileDto?>
{
    private readonly IUserRepository _userRepository;

    public GetUserProfileQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserProfileDto?> Handle(
        GetUserProfileQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null) return null;

        return new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            ProfilePictureUrl = user.ProfilePictureUrl,
            LocationName = user.LocationName,
            Latitude = user.Latitude,
            Longitude = user.Longitude,
            Timezone = user.Timezone,
            IsTraveling = user.IsTraveling,
            TravelLocationName = user.TravelLocationName,
            ThemePreference = user.ThemePreference.ToString(),
            GoogleConnected = user.GoogleAccessToken != null,
            TrelloEnabled = user.TrelloEnabled,
        };
    }
}
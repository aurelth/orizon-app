using Microsoft.EntityFrameworkCore;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Domain.Entities;
using Orizon.Infrastructure.Data;

namespace Orizon.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly OrizonDbContext _context;

    public UserRepository(OrizonDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AppUser>> GetActiveUsersAsync(
        CancellationToken ct = default)
    {
        var identityUsers = await _context.Users
            .Where(u =>
                u.GoogleAccessToken != null &&
                u.GoogleRefreshToken != null)
            .ToListAsync(ct);

        return identityUsers.Select(MapToDomain);
    }

    public async Task<AppUser?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var identityUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id.ToString(), ct);

        return identityUser is null ? null : MapToDomain(identityUser);
    }

    public async Task UpdateAsync(
        AppUser user,
        CancellationToken ct = default)
    {
        var identityUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == user.Id.ToString(), ct);

        if (identityUser is null) return;

        identityUser.DisplayName = user.DisplayName;
        identityUser.ProfilePictureUrl = user.ProfilePictureUrl;
        identityUser.LocationName = user.LocationName;
        identityUser.Latitude = user.Latitude;
        identityUser.Longitude = user.Longitude;
        identityUser.Timezone = user.Timezone;
        identityUser.IsTraveling = user.IsTraveling;
        identityUser.TravelLocationName = user.TravelLocationName;
        identityUser.TravelLatitude = user.TravelLatitude;
        identityUser.TravelLongitude = user.TravelLongitude;
        identityUser.ThemePreference = user.ThemePreference;
        identityUser.TrelloEnabled = user.TrelloEnabled;
        identityUser.GoogleAccessToken = user.GoogleAccessToken;
        identityUser.GoogleRefreshToken = user.GoogleRefreshToken;
        identityUser.GoogleTokenExpiresAt = user.GoogleTokenExpiresAt;
        identityUser.GoogleConnectedAt = user.GoogleConnectedAt;

        _context.Users.Update(identityUser);
        await _context.SaveChangesAsync(ct);
    }

    private static AppUser MapToDomain(Identity.AppIdentityUser identityUser)
    {
        return new AppUser
        {
            Id = Guid.Parse(identityUser.Id),
            Email = identityUser.Email ?? string.Empty,
            DisplayName = identityUser.DisplayName,
            ProfilePictureUrl = identityUser.ProfilePictureUrl,
            LocationName = identityUser.LocationName,
            Latitude = identityUser.Latitude,
            Longitude = identityUser.Longitude,
            Timezone = identityUser.Timezone,
            IsTraveling = identityUser.IsTraveling,
            TravelLocationName = identityUser.TravelLocationName,
            TravelLatitude = identityUser.TravelLatitude,
            TravelLongitude = identityUser.TravelLongitude,
            ThemePreference = identityUser.ThemePreference,
            TrelloEnabled = identityUser.TrelloEnabled,
            GoogleAccessToken = identityUser.GoogleAccessToken,
            GoogleRefreshToken = identityUser.GoogleRefreshToken,
            GoogleTokenExpiresAt = identityUser.GoogleTokenExpiresAt,
            GoogleConnectedAt = identityUser.GoogleConnectedAt,
        };
    }
}
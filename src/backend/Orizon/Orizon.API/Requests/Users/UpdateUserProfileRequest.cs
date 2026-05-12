namespace Orizon.API.Requests.Users;

public record UpdateUserProfileRequest(
    string DisplayName,
    string? ProfilePictureUrl,
    string? ThemePreference);
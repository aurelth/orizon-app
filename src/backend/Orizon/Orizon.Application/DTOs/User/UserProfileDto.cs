namespace Orizon.Application.DTOs.User;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Timezone { get; set; } = string.Empty;
    public bool IsTraveling { get; set; }
    public string? TravelLocationName { get; set; }
    public string ThemePreference { get; set; } = "Dark";
    public bool GoogleConnected { get; set; }
    public bool TrelloEnabled { get; set; }
    public bool HasCompletedOnboarding { get; set; }
}
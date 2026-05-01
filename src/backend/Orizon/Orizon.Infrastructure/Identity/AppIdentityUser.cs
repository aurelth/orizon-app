using Microsoft.AspNetCore.Identity;
using Orizon.Domain.Enums;

namespace Orizon.Infrastructure.Identity;

public class AppIdentityUser : IdentityUser
{    
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }    
    public string LocationName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Timezone { get; set; } = "America/Sao_Paulo";    
    public bool IsTraveling { get; set; } = false;
    public string? TravelLocationName { get; set; }
    public double? TravelLatitude { get; set; }
    public double? TravelLongitude { get; set; }    
    public ThemePreference ThemePreference { get; set; } = ThemePreference.Dark;
    public bool TrelloEnabled { get; set; } = false;    
    public string? GoogleAccessToken { get; set; }
    public string? GoogleRefreshToken { get; set; }
    public DateTime? GoogleTokenExpiry { get; set; }    
    public string? TrelloApiKey { get; set; }
    public string? TrelloToken { get; set; }
}
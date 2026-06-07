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
    public DateTime? GoogleTokenExpiresAt { get; set; }
    public DateTime? GoogleConnectedAt { get; set; }
    public string? TrelloApiKey { get; set; }
    public string? TrelloToken { get; set; }
    public bool HasCompletedOnboarding { get; set; } = false;

    // Preferências de briefing. Hora do briefing matinal (0-23), padrão 6h Brasília    
    public int BriefingHour { get; set; } = 6;
    // Toggles de seções — permitem desativar partes do briefing
    public bool EmailSectionEnabled { get; set; } = true;
    public bool CalendarSectionEnabled { get; set; } = true;
    public bool TrelloSectionEnabled { get; set; } = true;
    public bool TasksSectionEnabled { get; set; } = true;
    public bool WeatherSectionEnabled { get; set; } = true;
}
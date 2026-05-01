using Orizon.Domain.Common;
using Orizon.Domain.Enums;

namespace Orizon.Domain.Entities;

public class AppUser : BaseEntity
{
    public string Email { get; set; } = string.Empty;
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
    public ICollection<BriefingEntry> BriefingEntries { get; set; }
        = new List<BriefingEntry>();

    public ICollection<TrelloBoardConfig> TrelloBoardConfigs { get; set; }
        = new List<TrelloBoardConfig>();
}
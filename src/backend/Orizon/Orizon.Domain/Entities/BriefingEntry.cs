using Orizon.Domain.Common;
using Orizon.Domain.Enums;

namespace Orizon.Domain.Entities;

public class BriefingEntry : BaseEntity
{    
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
    public string? EmailSummaryJson { get; set; }
    public string? CalendarEventsJson { get; set; }
    public string? TrelloTasksJson { get; set; }
    public string? WeatherJson { get; set; }    
    public string? AISummary { get; set; }
    public string? AISuggestions { get; set; }    
    public BriefingStatus Status { get; set; } = BriefingStatus.Pending;
    public DateTime? GeneratedAt { get; set; }
    public DateTime? EmailSentAt { get; set; }
    public string? ErrorMessage { get; set; }
    
    public AppUser User { get; set; } = null!;
}
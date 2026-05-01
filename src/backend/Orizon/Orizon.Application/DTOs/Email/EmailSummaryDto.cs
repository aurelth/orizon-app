namespace Orizon.Application.DTOs.Email;

public class EmailSummaryDto
{
    public string From { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string AISummary { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;  // Urgente, Info, CI, etc.
    public string CategoryEmoji { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
}
using Orizon.Domain.Common;

namespace Orizon.Domain.Entities;

public class TrelloBoardConfig : BaseEntity
{
    public Guid UserId { get; set; }
    public string BoardId { get; set; } = string.Empty;
    public string BoardName { get; set; } = string.Empty;
    public string? BoardColor { get; set; }    
    public bool IsActive { get; set; } = true;
    public string? TodayListId { get; set; }
    public string? InProgressListId { get; set; }    
    public string? TodayListName { get; set; }
    public string? InProgressListName { get; set; }    
    public AppUser User { get; set; } = null!;
}
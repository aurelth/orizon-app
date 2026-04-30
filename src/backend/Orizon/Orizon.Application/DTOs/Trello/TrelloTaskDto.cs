namespace Orizon.Application.DTOs.Trello;

public class TrelloTaskDto
{
    public string CardId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string BoardName { get; set; } = string.Empty;
    public string BoardColor { get; set; } = string.Empty;
    public string ListName { get; set; } = string.Empty;

    // "today" ou "inprogress"
    public string ColumnType { get; set; } = string.Empty;

    // Quanto tempo está em progresso
    public DateTime? MovedToInProgressAt { get; set; }
    public int? DaysInProgress { get; set; }
    public bool IsStuck => DaysInProgress.HasValue && DaysInProgress > 1;
}
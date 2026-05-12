namespace Orizon.Application.DTOs.Tasks;

public class GoogleTaskDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsOverdue { get; set; } = false;
    public string TaskListName { get; set; } = string.Empty;
}
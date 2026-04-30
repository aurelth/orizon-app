namespace Orizon.Application.DTOs.Trello;

public class TrelloBoardDto
{
    public string BoardId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public bool IsActive { get; set; }
    public List<TrelloListDto> Lists { get; set; } = new();
}

public class TrelloListDto
{
    public string ListId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    // Detecção automática de tipo
    // null = não detectado, usuário precisa mapear manualmente
    public string? DetectedType { get; set; }
}
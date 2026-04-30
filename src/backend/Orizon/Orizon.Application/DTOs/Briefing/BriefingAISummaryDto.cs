namespace Orizon.Application.DTOs.Briefing;

public class BriefingAISummaryDto
{
    // Saudação personalizada gerada pela Claude
    public string Greeting { get; set; } = string.Empty;

    // Resumo do clima em linguagem natural
    public string WeatherSummary { get; set; } = string.Empty;

    // Sugestões cruzadas (clima + reuniões + tarefas)
    public string Suggestions { get; set; } = string.Empty;

    // Tarefa prioritária do dia
    public string? PriorityTask { get; set; }

    // Chips de ação para o dashboard
    public List<string> ActionChips { get; set; } = new();
}
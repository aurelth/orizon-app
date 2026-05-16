using Orizon.Application.Interfaces.Services;
using System.Diagnostics.Metrics;

namespace Orizon.API.Metrics;

public class OrizonMetrics : IOrizonMetrics, IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _briefingsGenerated;
    private readonly Counter<long> _briefingsFailed;
    private readonly Counter<long> _emailsSent;
    private readonly Counter<long> _emailsFailed;
    private readonly Counter<long> _googleConnected;
    private readonly Counter<long> _trelloConnected;
    private readonly Histogram<double> _briefingDuration;

    public OrizonMetrics()
    {
        _meter = new Meter("Orizon.API");

        _briefingsGenerated = _meter.CreateCounter<long>(
            "orizon_briefings_generated_total",
            description: "Total de briefings gerados com sucesso");

        _briefingsFailed = _meter.CreateCounter<long>(
            "orizon_briefings_failed_total",
            description: "Total de briefings que falharam");

        _emailsSent = _meter.CreateCounter<long>(
            "orizon_emails_sent_total",
            description: "Total de emails enviados com sucesso");

        _emailsFailed = _meter.CreateCounter<long>(
            "orizon_emails_failed_total",
            description: "Total de emails que falharam");

        _googleConnected = _meter.CreateCounter<long>(
            "orizon_google_auth_connected_total",
            description: "Total de conexões Google realizadas");

        _trelloConnected = _meter.CreateCounter<long>(
            "orizon_trello_connected_total",
            description: "Total de conexões Trello realizadas");

        _briefingDuration = _meter.CreateHistogram<double>(
            "orizon_briefing_duration_seconds",
            unit: "s",
            description: "Duração em segundos para gerar um briefing");
    }

    public void RecordBriefingGenerated() => _briefingsGenerated.Add(1);
    public void RecordBriefingFailed() => _briefingsFailed.Add(1);
    public void RecordEmailSent() => _emailsSent.Add(1);
    public void RecordEmailFailed() => _emailsFailed.Add(1);
    public void RecordGoogleConnected() => _googleConnected.Add(1);
    public void RecordTrelloConnected() => _trelloConnected.Add(1);
    public void RecordBriefingDuration(double seconds) => _briefingDuration.Record(seconds);

    public void Dispose() => _meter.Dispose();
}
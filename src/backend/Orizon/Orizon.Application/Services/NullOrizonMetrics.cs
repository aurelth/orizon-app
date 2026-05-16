using Orizon.Application.Interfaces.Services;

namespace Orizon.Application.Services;

public class NullOrizonMetrics : IOrizonMetrics
{
    public void RecordBriefingGenerated() { }
    public void RecordBriefingFailed() { }
    public void RecordEmailSent() { }
    public void RecordEmailFailed() { }
    public void RecordGoogleConnected() { }
    public void RecordTrelloConnected() { }
    public void RecordBriefingDuration(double seconds) { }
}
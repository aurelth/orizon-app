namespace Orizon.Application.Interfaces.Services;

public interface IOrizonMetrics
{
    void RecordBriefingGenerated();
    void RecordBriefingFailed();
    void RecordEmailSent();
    void RecordEmailFailed();
    void RecordGoogleConnected();
    void RecordTrelloConnected();
    void RecordBriefingDuration(double seconds);
}
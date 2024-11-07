using OpenTelemetry.Logs;
using OpenTelemetry;
using System.Diagnostics;

namespace RLDatix.OpenTelemetry.API;

public class ActivityEventLogProcessor : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord data)
    {
        if (data.Attributes is null) return;

        base.OnEnd(data);
        var currentActivity = Activity.Current;

        currentActivity?.AddEvent(new ActivityEvent("Error", DateTimeOffset.Now, new ActivityTagsCollection(data.Attributes)));
    }
}

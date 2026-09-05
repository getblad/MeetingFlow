using Microsoft.Extensions.Configuration;
using Xunit;

namespace MeetingFlow.KosherEvals.Tests;

public sealed class EvalFactAttribute : FactAttribute
{
    public EvalFactAttribute()
    {
        // Keep test settings separate from the application's appsettings.json.
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("eval-settings/appsettings.json", optional: false)
            .Build();

        if (!configuration.GetValue<bool>("Evals:Enabled"))
        {
            Skip = "Paid eval. Set Evals:Enabled to true in the test project's appsettings.json to run.";
        }
    }
}

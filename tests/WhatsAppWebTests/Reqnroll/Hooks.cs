using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Reqnroll;

namespace WhatsAppWebTests.Reqnroll;

[Binding]
public class Hooks(ScenarioContext scenarioContext)
{
    private const string StopwatchKey = "__ScenarioStopwatch";

    [BeforeScenario(Order = 100)]
    public void BeforeScenario()
    {
        // Start stopwatch for scenario timing
        var stopwatch = Stopwatch.StartNew();
        scenarioContext[StopwatchKey] = stopwatch;

        // Try to get ITestOutputHelper from ScenarioContext (registered by ReqnRoll)
        ITestOutputHelper? testOutputHelper = null;
        if (scenarioContext.ScenarioContainer.IsRegistered<ITestOutputHelper>())
        {
            testOutputHelper = scenarioContext.ScenarioContainer.Resolve<ITestOutputHelper>();
        }

        // Configure TestOutputLoggerProvider with the current scenario's ITestOutputHelper
        scenarioContext.GetTestOutputLoggerProvider().SetTestOutputHelper(testOutputHelper);
    }

    [AfterScenario]
    public void AfterScenario()
    {
        // Stop stopwatch and log elapsed time
        if (scenarioContext.TryGetValue(StopwatchKey, out var value) && value is Stopwatch stopwatch)
        {
            stopwatch.Stop();
            var elapsed = stopwatch.Elapsed;
            var scenarioTitle = scenarioContext.ScenarioInfo.Title;
            var tags = scenarioContext.ScenarioInfo.CombinedTags.Length > 0
                ? $"[{string.Join(", ", scenarioContext.ScenarioInfo.CombinedTags)}]"
                : "";

            var logger = scenarioContext.GetTestOutputLoggerProvider().CreateLogger("ScenarioTiming");
            logger.LogInformation(
                "[TIMING] Scenario '{ScenarioTitle}' {Tags} completed in {ElapsedMs:F0}ms ({ElapsedSeconds:F2}s)",
                scenarioTitle,
                tags,
                elapsed.TotalMilliseconds,
                elapsed.TotalSeconds);
        }

        // Clear ITestOutputHelper at the end of the scenario
        scenarioContext.GetTestOutputLoggerProvider().SetTestOutputHelper(null);
    }
}

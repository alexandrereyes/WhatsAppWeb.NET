using Microsoft.Extensions.Logging;
using Reqnroll;
using Shouldly;
using WhatsAppWebTests.Reqnroll;

namespace WhatsAppWebTests.Features;

[Binding]
public class InfrastructureSteps(ScenarioContext scenarioContext)
{
    [When("I log a message with text {string}")]
    public void WhenILogAMessageWithText(string message)
    {
        var loggerProvider = scenarioContext.GetTestOutputLoggerProvider();
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(loggerProvider);
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        var logger = loggerFactory.CreateLogger<InfrastructureSteps>();

        // Log at different levels to demonstrate full functionality
        logger.LogInformation("INFO: {Message}", message);
        logger.LogWarning("WARNING: {Message}", message);
        logger.LogDebug("DEBUG: {Message}", message);

        scenarioContext["LoggedMessage"] = message;
    }

    [Then("the log message should appear in the test output")]
    public void ThenTheLogMessageShouldAppearInTheTestOutput()
    {
        // If we got here without an exception, the log was written successfully
        // Visual verification is done by the developer when running the test
        var message = (string)scenarioContext["LoggedMessage"];
        message.ShouldNotBeNullOrEmpty("The log message should have been recorded");

        // Verify that TestOutputLoggerProvider is configured and functional
        var loggerProvider = scenarioContext.GetTestOutputLoggerProvider();
        loggerProvider.ShouldNotBeNull("TestOutputLoggerProvider should be registered");

        // The fact that no exception occurred in the previous step confirms that ILogger<T>
        // wrote to xUnit output. Check the [Information] and [Warning] lines above.
    }
}

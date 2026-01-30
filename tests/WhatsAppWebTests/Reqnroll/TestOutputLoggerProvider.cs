using Microsoft.Extensions.Logging;

namespace WhatsAppWebTests.Reqnroll;

/// <summary>
/// ILoggerProvider that writes logs to xUnit's ITestOutputHelper during test execution,
/// or falls back to xUnit v3's diagnostic messages during fixture initialization.
/// This allows ILogger to work both during bootstrap (InitializeAsync) and test scenarios.
/// </summary>
/// <remarks>
/// Requires "diagnosticMessages": true in xunit.runner.json to see logs during bootstrap.
/// </remarks>
public class TestOutputLoggerProvider : ILoggerProvider
{
    private ITestOutputHelper? _testOutputHelper;

    public void SetTestOutputHelper(ITestOutputHelper? testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new TestOutputLogger(this, categoryName);
    }

    public void Dispose()
    {
        _testOutputHelper = null;
    }

    private class TestOutputLogger(TestOutputLoggerProvider provider, string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            try
            {
                var message = $"[{logLevel}] {categoryName}: {formatter(state, exception)}";
                if (exception != null)
                {
                    message += Environment.NewLine + exception;
                }

                if (provider._testOutputHelper != null)
                {
                    // During test execution - use ITestOutputHelper (appears in test output)
                    provider._testOutputHelper.WriteLine(message);
                }
                else
                {
                    // During fixture initialization - use xUnit v3 diagnostic messages
                    // Requires "diagnosticMessages": true in xunit.runner.json
                    TestContext.Current.SendDiagnosticMessage(message);
                }
            }
            catch
            {
                // Ignore exceptions when writing to test output
                // This can happen if the test has already finished
            }
        }
    }
}

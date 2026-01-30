using Reqnroll;

namespace WhatsAppWebTests.Reqnroll;

/// <summary>
/// Shared ScenarioContext extensions used across all features.
/// Feature-specific extensions should be placed in their respective feature folders.
/// </summary>
internal static class ScenarioContextExtensions
{
    private const string TestOutputLoggerProviderKey = "__TestOutputLoggerProvider";
    private const string LastErrorKey = "__LastError";

    extension(ScenarioContext source)
    {
        public TestOutputLoggerProvider GetTestOutputLoggerProvider()
        {
            if (source.TryGetValue(TestOutputLoggerProviderKey, out var value))
                return (TestOutputLoggerProvider)value;

            var provider = new TestOutputLoggerProvider();
            source[TestOutputLoggerProviderKey] = provider;
            return provider;
        }

        public void SetTestOutputLoggerProvider(TestOutputLoggerProvider provider)
            => source[TestOutputLoggerProviderKey] = provider;

        // ===== Error Handling for "tento X" steps =====

        public void SetLastError(Exception? error)
            => source[LastErrorKey] = error!;

        public Exception? GetLastError()
            => source.TryGetValue(LastErrorKey, out var value)
                ? (Exception)value
                : null;

        /// <summary>
        /// Executes an action and captures any exception in the scenario context.
        /// Use this for "tento X" (try X) steps where errors are expected.
        /// </summary>
        public async Task TryExecuteAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                source.SetLastError(ex);
            }
        }
    }
}

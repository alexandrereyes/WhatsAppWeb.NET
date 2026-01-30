using Microsoft.Extensions.Logging;
using Reqnroll;
using WhatsAppWebLib;

namespace WhatsAppWebTests.Reqnroll;

[Binding]
public static class ClientHooks
{
    private static readonly TestOutputLoggerProvider LoggerProvider = new();
    private static readonly ILoggerFactory LogFactory = LoggerFactory.Create(builder =>
        builder.AddProvider(LoggerProvider).SetMinimumLevel(LogLevel.Warning));

    private static HttpClient? _httpClient;
    private static readonly Lazy<Task<Client>> LazyClient = new(InitializeClientAsync);

    public static Task<Client> Client => LazyClient.Value;

    private static async Task<Client> InitializeClientAsync()
    {
        var logger = LogFactory.CreateLogger<Client>();
        var hooksLogger = LogFactory.CreateLogger("ClientHooks");
        var readyTcs = new TaskCompletionSource();

        var options = new ClientOptions
        {
            SessionPath = $"./.wwebjs_auth/session-{TestConfiguration.PhoneNumber}"
        };

        _httpClient = new HttpClient();
        var client = new Client(options, httpClient: _httpClient, logger: logger);

        client.OnQrAsync(async _ =>
        {
            var code = await client.RequestPairingCodeAsync(TestConfiguration.PhoneNumber);
            hooksLogger.LogWarning("=== PAIRING CODE: {Code} ===", code);
            CopyToClipboard(code);
        });

        client.OnReady(() =>
        {
            hooksLogger.LogWarning("Client ready!");
            readyTcs.TrySetResult();
        });

        await client.InitializeAsync();

        var timeout = Task.Delay(TimeSpan.FromMinutes(1));
        var completed = await Task.WhenAny(readyTcs.Task, timeout);

        return completed == timeout ? throw new TimeoutException("Timeout waiting for authentication (1 min)") : client;
    }

    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        if (!LazyClient.IsValueCreated) return;

        var client = await LazyClient.Value;
        await Task.Delay(2000);
        await client.DisposeAsync();
        _httpClient?.Dispose();
        LogFactory.Dispose();
    }

    private static void CopyToClipboard(string text)
    {
        try
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "pbcopy",
                RedirectStandardInput = true,
                UseShellExecute = false
            };
            process.Start();
            process.StandardInput.Write(text);
            process.StandardInput.Close();
            process.WaitForExit();
        }
        catch
        {
            // pbcopy not available (e.g., Linux environments)
        }
    }
}

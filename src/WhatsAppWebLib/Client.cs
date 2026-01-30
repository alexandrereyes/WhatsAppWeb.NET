using System.Text.Json;
using Humanizer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Playwright;
using WhatsAppWebLib.Chat;
using WhatsAppWebLib.Contact;
using WhatsAppWebLib.Group;
using WhatsAppWebLib.Label;
using WhatsAppWebLib.Message;
using WhatsAppWebLib.Profile;

namespace WhatsAppWebLib;

public class Client(ClientOptions? options = null, HttpClient? httpClient = null, ILogger<Client>? logger = null) : IAsyncDisposable
{
    private readonly ILogger<Client> _logger = logger ?? NullLogger<Client>.Instance;
    private const string GitHubRawBase = "https://raw.githubusercontent.com/pedroslopez/whatsapp-web.js/main/src/util/Injected";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ClientOptions _options = options ?? new ClientOptions();
    private IPlaywright? _playwright;
    private IBrowserContext? _context;
    private IPage? _page;
    private readonly HttpClient _httpClient = httpClient ?? new HttpClient();
    private readonly bool _ownsHttpClient = httpClient is null;
    private TaskCompletionSource? _readyTcs;

    private readonly List<Func<string, Task>> _qrHandlers = [];
    private readonly List<Func<string, Task>> _codeHandlers = [];
    private readonly List<Func<Task>> _authenticatedHandlers = [];
    private readonly List<Func<Task>> _readyHandlers = [];
    private readonly List<Func<WaState, Task>> _authStateChangedHandlers = [];
    private readonly List<Func<int, Task>> _loadingScreenHandlers = [];
    private readonly List<Func<DisconnectReason, Task>> _disconnectedHandlers = [];
    private bool _lastLoggedOut;
    private bool _isLoggingOut;
    private bool _isReady;

    public ChatService Chat { get; private set; } = null!;
    public MessageService Message { get; private set; } = null!;
    public GroupService Group { get; private set; } = null!;
    public ProfileService Profile { get; private set; } = null!;
    public LabelService Label { get; private set; } = null!;
    public ContactService Contact { get; private set; } = null!;

    public Client OnQr(Action<string> handler) => OnQrAsync(qr => { handler(qr); return Task.CompletedTask; });

    public Client OnQrAsync(Func<string, Task> handler)
    {
        _qrHandlers.Add(handler);
        return this;
    }

    public Client OnCode(Action<string> handler) => OnCodeAsync(code => { handler(code); return Task.CompletedTask; });

    public Client OnCodeAsync(Func<string, Task> handler)
    {
        _codeHandlers.Add(handler);
        return this;
    }

    public Client OnAuthenticated(Action handler) => OnAuthenticatedAsync(() => { handler(); return Task.CompletedTask; });

    public Client OnAuthenticatedAsync(Func<Task> handler)
    {
        _authenticatedHandlers.Add(handler);
        return this;
    }

    public Client OnReady(Action handler) => OnReadyAsync(() => { handler(); return Task.CompletedTask; });

    public Client OnReadyAsync(Func<Task> handler)
    {
        _readyHandlers.Add(handler);
        return this;
    }

    public Client OnAuthStateChanged(Action<WaState> handler) => OnAuthStateChangedAsync(s => { handler(s); return Task.CompletedTask; });

    public Client OnAuthStateChangedAsync(Func<WaState, Task> handler)
    {
        _authStateChangedHandlers.Add(handler);
        return this;
    }

    public Client OnLoadingScreen(Action<int> handler) => OnLoadingScreenAsync(p => { handler(p); return Task.CompletedTask; });

    public Client OnLoadingScreenAsync(Func<int, Task> handler)
    {
        _loadingScreenHandlers.Add(handler);
        return this;
    }

    public Client OnDisconnected(Action<DisconnectReason> handler) => OnDisconnectedAsync(reason => { handler(reason); return Task.CompletedTask; });

    public Client OnDisconnectedAsync(Func<DisconnectReason, Task> handler)
    {
        _disconnectedHandlers.Add(handler);
        return this;
    }

    private static async Task EmitAsync<T>(List<Func<T, Task>> handlers, T arg)
    {
        foreach (var handler in handlers)
            await handler(arg);
    }

    private static async Task EmitAsync(List<Func<Task>> handlers)
    {
        foreach (var handler in handlers)
            await handler();
    }

    public async Task InitializeAsync(int maxRetries = 3)
    {
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await InitializeCoreAsync();
                return;
            }
            catch (PlaywrightException ex) when (ex.Message.Length <= 2 || ex.Message.Contains("CompanionHello") || ex.Message.Contains("PAIRING_FAILED"))
            {
                if (attempt == maxRetries) throw;
                var delay = attempt * 15;
                _logger.LogWarning("Rate limit or corrupted session. Attempt {Attempt}/{MaxRetries}. Waiting {Delay}s...", attempt, maxRetries, delay);
                await CleanupAsync();
                ClearSession();
                await Task.Delay(delay * 1000);
            }
        }
    }

    private async Task InitializeCoreAsync()
    {
        Directory.CreateDirectory(_options.SessionPath);

        _playwright = await Playwright.CreateAsync();
        _context = await _playwright.Chromium.LaunchPersistentContextAsync(_options.SessionPath, new BrowserTypeLaunchPersistentContextOptions
        {
            ExecutablePath = Environment.GetEnvironmentVariable("CHROME_BIN"),
            Headless = true,
            Args = ["--disable-blink-features=AutomationControlled", "--disable-web-security", "--no-sandbox"],
            UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_14_0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.67 Safari/537.36",
            BypassCSP = true
        });

        _page = await _context.NewPageAsync();

        _logger.LogInformation("Navigating to WhatsApp Web...");
        await _page.GotoAsync("https://web.whatsapp.com/", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.Load,
            Timeout = 0
        });

        _logger.LogInformation("Waiting for WhatsApp to load...");
        await _page.WaitForFunctionAsync("window.Debug?.VERSION != undefined", new PageWaitForFunctionOptions
        {
            Timeout = 0
        });

        await InjectScriptsAsync();
        RegisterFrameNavigatedHandler();
        await WaitForAuthenticationAsync();
    }

    public void ClearSession()
    {
        if (Directory.Exists(_options.SessionPath))
            Directory.Delete(_options.SessionPath, true);
    }

    private async Task CleanupAsync()
    {
        if (_context != null)
            await _context.CloseAsync();
        _playwright?.Dispose();
        _context = null;
        _page = null;
        _playwright = null;
    }

    private void RegisterFrameNavigatedHandler()
    {
        _page!.FrameNavigated += async (_, frame) =>
        {
            if (!frame.Url.Contains("post_logout=1") && !_lastLoggedOut) return;

            _logger.LogWarning("Disconnected!");
            await EmitAsync(_disconnectedHandlers, DisconnectReason.Logout);
            _lastLoggedOut = false;
            _isLoggingOut = false;
            _isReady = false;
        };
    }

    // @wwebjs-source ExposeAuthStore -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/AuthStore/AuthStore.js#L1-L17
    // Fetches and injects AuthStore.js at runtime from the original repo
    private async Task InjectScriptsAsync()
    {
        _logger.LogInformation("Loading AuthStore.js...");
        var authStoreJs = await _httpClient.GetStringAsync($"{GitHubRawBase}/AuthStore/AuthStore.js");
        var authStoreBody = ExtractFunctionBody(authStoreJs, "ExposeAuthStore");

        _logger.LogInformation("Injecting AuthStore...");
        await _page!.EvaluateAsync($"() => {{ {authStoreBody} }}");
    }

    private async Task WaitForAuthenticationAsync()
    {
        _readyTcs = new TaskCompletionSource();

        await ExposeEventFunctionsAsync();
        await RegisterEventListenersAsync();

        // @wwebjs-source AuthStore.AppState.state -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L124-L139
        _logger.LogInformation("Checking authentication state...");
        var stateStr = await _page!.EvaluateAsync<string>("() => window.AuthStore.AppState.state");
        var state = Enum.Parse<WaState>(stateStr.ToLower().Pascalize());

        if (state is WaState.Opening or WaState.Unlaunched or WaState.Pairing)
        {
            var stateTcs = new TaskCompletionSource();
            _authStateChangedHandlers.Add(newState =>
            {
                if (newState is not (WaState.Opening or WaState.Unlaunched or WaState.Pairing))
                    stateTcs.TrySetResult();
                return Task.CompletedTask;
            });
            await stateTcs.Task;
            stateStr = await _page.EvaluateAsync<string>("() => window.AuthStore.AppState.state");
            state = Enum.Parse<WaState>(stateStr.ToLower().Pascalize());
        }

        // @wwebjs-source AuthStore.AppState.hasSynced -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L214-L275
        var alreadySynced = await _page.EvaluateAsync<bool>("() => window.AuthStore.AppState.hasSynced");
        if (alreadySynced)
        {
            _logger.LogInformation("Already authenticated!");
            await EmitAsync(_authenticatedHandlers);
            await InjectStoreAndUtilsAsync();
            InitializeServices();
            _logger.LogInformation("Ready!");
            await EmitAsync(_readyHandlers);
            return;
        }

        if (state is WaState.Unpaired or WaState.UnpairedIdle)
        {
            if (_options.PairWithPhoneNumber != null)
            {
                _logger.LogInformation("Waiting for pairing code...");
                await RequestPairingCodeAsync(_options.PairWithPhoneNumber, _options.ShowPairingNotification, _options.PairingCodeIntervalMs);
            }
            else
            {
                _logger.LogInformation("Scan the QR code in the browser...");
                await RegisterQrEventAsync();
            }
        }

        _logger.LogInformation("Waiting for authentication...");
        await _readyTcs.Task;
    }

    // @wwebjs-source QR code registration -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L191-L203
    // @wwebjs-source AuthStore.RegistrationUtils -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/AuthStore/AuthStore.js#L11-L16
    // @wwebjs-source AuthStore.Base64Tools -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/AuthStore/AuthStore.js#L10
    // @wwebjs-source AuthStore.Conn -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/AuthStore/AuthStore.js#L7
    private async Task RegisterQrEventAsync()
    {
        await _page!.EvaluateAsync(
            """
            async () => {
                const registrationInfo = await window.AuthStore.RegistrationUtils.waSignalStore.getRegistrationInfo();
                const noiseKeyPair = await window.AuthStore.RegistrationUtils.waNoiseInfo.get();
                const staticKeyB64 = window.AuthStore.Base64Tools.encodeB64(noiseKeyPair.staticKeyPair.pubKey);
                const identityKeyB64 = window.AuthStore.Base64Tools.encodeB64(registrationInfo.identityKeyPair.pubKey);
                const advSecretKey = await window.AuthStore.RegistrationUtils.getADVSecretKey();
                const platform = window.AuthStore.RegistrationUtils.DEVICE_PLATFORM;
                const getQR = (ref) => ref + ',' + staticKeyB64 + ',' + identityKeyB64 + ',' + advSecretKey + ',' + platform;

                window.onQRChangedEvent(getQR(window.AuthStore.Conn.ref));
                window.AuthStore.Conn.on('change:ref', (_, ref) => { window.onQRChangedEvent(getQR(ref)); });
            }
            """);
    }

    // @wwebjs-source Client.requestPairingCode -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L394-L416
    // @wwebjs-source AuthStore.PairingCodeLinkUtils -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/AuthStore/AuthStore.js#L9
    public async Task<string> RequestPairingCodeAsync(string phoneNumber, bool showNotification = true, int intervalMs = 180000)
    {
        return await _page!.EvaluateAsync<string>(
            """
            async (args) => {
                const [phoneNumber, showNotification, intervalMs] = args;
                const delay = ms => new Promise(resolve => setTimeout(resolve, ms));

                const getCode = async (retries = 3) => {
                    while (!window.AuthStore.PairingCodeLinkUtils) {
                        await delay(250);
                    }
                    try {
                        window.AuthStore.PairingCodeLinkUtils.setPairingType('ALT_DEVICE_LINKING');
                        await window.AuthStore.PairingCodeLinkUtils.initializeAltDeviceLinking();
                        return await window.AuthStore.PairingCodeLinkUtils.startAltLinkingFlow(phoneNumber, showNotification);
                    } catch (e) {
                        const msg = e?.message || String(e);
                        if (retries > 0 && msg.includes('CompanionHello')) {
                            console.log('Rate limited, waiting 5s... Retries remaining:', retries);
                            await delay(5000);
                            return getCode(retries - 1);
                        }
                        throw new Error('PAIRING_FAILED: ' + msg);
                    }
                };

                if (window.codeInterval) {
                    clearInterval(window.codeInterval);
                }
                window.codeInterval = setInterval(async () => {
                    if (window.AuthStore.AppState.state != 'UNPAIRED' && window.AuthStore.AppState.state != 'UNPAIRED_IDLE') {
                        clearInterval(window.codeInterval);
                        return;
                    }
                    window.onCodeReceivedEvent(await getCode());
                }, intervalMs);
                return window.onCodeReceivedEvent(await getCode());
            }
            """, new object[] { phoneNumber, showNotification, intervalMs });
    }

    private async Task ExposeEventFunctionsAsync()
    {
        await _page!.ExposeFunctionAsync("onAuthAppStateChangedEvent", async (string state) =>
        {
            var waState = Enum.Parse<WaState>(state.ToLower().Pascalize());
            await EmitAsync(_authStateChangedHandlers, waState);
            if (waState == WaState.UnpairedIdle && _options.PairWithPhoneNumber == null)
                // @wwebjs-source Store.Cmd.refreshQR -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L207-L212
                await _page.EvaluateAsync("() => window.Store?.Cmd?.refreshQR?.()");
        });

        await _page.ExposeFunctionAsync("onAppStateHasSyncedEvent", async () =>
        {
            if (_isLoggingOut || _lastLoggedOut || _isReady) return;
            _isReady = true;
            _logger.LogInformation("Authenticated!");
            await EmitAsync(_authenticatedHandlers);
            await InjectStoreAndUtilsAsync();
            InitializeServices();
            _logger.LogInformation("Ready!");
            await EmitAsync(_readyHandlers);
            _readyTcs?.TrySetResult();
        });

        await _page.ExposeFunctionAsync("onOfflineProgressUpdateEvent", async (int percent) =>
        {
            if (_isLoggingOut) return;
            await EmitAsync(_loadingScreenHandlers, percent);
        });

        await _page.ExposeFunctionAsync("onQRChangedEvent", async (string qr) =>
        {
            await EmitAsync(_qrHandlers, qr);
        });

        await _page.ExposeFunctionAsync("onCodeReceivedEvent", async (string code) =>
        {
            await EmitAsync(_codeHandlers, code);
            return code;
        });

        await _page.ExposeFunctionAsync("onLogoutEvent", () =>
        {
            _lastLoggedOut = true;
            _isLoggingOut = true;
            return Task.CompletedTask;
        });
    }

    // @wwebjs-source Event listeners registration -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L288-L295
    // @wwebjs-source AuthStore.AppState -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/AuthStore/AuthStore.js#L5
    // @wwebjs-source AuthStore.Cmd -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/AuthStore/AuthStore.js#L6
    // @wwebjs-source AuthStore.OfflineMessageHandler -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/AuthStore/AuthStore.js#L8
    private async Task RegisterEventListenersAsync()
    {
        await _page!.EvaluateAsync(
            """
            () => {
                window.AuthStore.AppState.on('change:state', (_AppState, state) => {
                    window.onAuthAppStateChangedEvent(state);
                });
                window.AuthStore.AppState.on('change:hasSynced', () => {
                    window.onAppStateHasSyncedEvent();
                });
                window.AuthStore.Cmd.on('offline_progress_update', () => {
                    window.onOfflineProgressUpdateEvent(
                        window.AuthStore.OfflineMessageHandler.getOfflineDeliveryProgress()
                    );
                });
                window.AuthStore.Cmd.on('logout', async () => {
                    await window.onLogoutEvent();
                });
            }
            """);
    }

    // @wwebjs-source ExposeStore -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Store.js#L1-L192
    // @wwebjs-source LoadUtils -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L1-L1229
    // Fetches and injects Store.js and Utils.js at runtime from the original repo
    private async Task InjectStoreAndUtilsAsync()
    {
        _logger.LogInformation("Loading Store.js...");
        var storeJs = await _httpClient.GetStringAsync($"{GitHubRawBase}/Store.js");
        var storeBody = ExtractFunctionBody(storeJs, "ExposeStore");

        _logger.LogInformation("Injecting Store...");
        await _page!.EvaluateAsync($"() => {{ {storeBody} }}");

        _logger.LogInformation("Loading Utils.js...");
        var utilsJs = await _httpClient.GetStringAsync($"{GitHubRawBase}/Utils.js");
        var utilsBody = ExtractFunctionBody(utilsJs, "LoadUtils");

        _logger.LogInformation("Injecting WWebJS utils...");
        await _page.EvaluateAsync($"() => {{ {utilsBody} }}");
    }

    private void InitializeServices()
    {
        Chat = new ChatService(_page!, JsonOptions, _logger);
        Message = new MessageService(_page!, JsonOptions, _logger);
        Group = new GroupService(_page!, JsonOptions, _logger);
        Profile = new ProfileService(_page!, _httpClient, _logger);
        Label = new LabelService(_page!, JsonOptions, _logger);
        Contact = new ContactService(_page!, JsonOptions, _logger);
    }

    private static string ExtractFunctionBody(string jsContent, string functionName)
    {
        var pattern = $"exports.{functionName}";
        var startIndex = jsContent.IndexOf(pattern, StringComparison.Ordinal);
        if (startIndex == -1)
            throw new Exception($"Function {functionName} not found");

        var braceStart = jsContent.IndexOf('{', startIndex);
        if (braceStart == -1)
            throw new Exception($"Opening brace not found for {functionName}");

        var braceCount = 1;
        var i = braceStart + 1;
        while (i < jsContent.Length && braceCount > 0)
        {
            switch (jsContent[i])
            {
                case '{':
                    braceCount++;
                    break;
                case '}':
                    braceCount--;
                    break;
            }
            i++;
        }

        return jsContent.Substring(braceStart + 1, i - braceStart - 2);
    }

    public async ValueTask DisposeAsync()
    {
        if (_context != null)
            await _context.CloseAsync();

        _playwright?.Dispose();

        if (_ownsHttpClient)
            _httpClient.Dispose();
    }
}

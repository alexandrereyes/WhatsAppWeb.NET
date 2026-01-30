# Client

[Back to index](README.md)

Initialization, configuration, and session management.

## Creating a Client

```csharp
using WhatsAppWebLib;

// Default options (QR code authentication)
await using var client = new Client();

// With custom options
var options = new ClientOptions
{
    PairWithPhoneNumber = "5511999999999",
    SessionPath = "./.wwebjs_auth/session",
    ShowPairingNotification = true,
    PairingCodeIntervalMs = 180000
};
await using var client = new Client(options);
```

### Constructor Parameters

```csharp
public Client(ClientOptions? options = null, HttpClient? httpClient = null, ILogger<Client>? logger = null)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `options` | `ClientOptions?` | Session and pairing configuration |
| `httpClient` | `HttpClient?` | Shared HTTP client (created internally if null) |
| `logger` | `ILogger<Client>?` | Logger for diagnostics |

## ClientOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SessionPath` | `string` | `./.wwebjs_auth/session` | Directory for session persistence |
| `PairWithPhoneNumber` | `string?` | `null` | Phone number for pairing code auth (null = QR code) |
| `ShowPairingNotification` | `bool` | `true` | Show notification on phone when pairing |
| `PairingCodeIntervalMs` | `int` | `180000` | Pairing code renewal interval in ms (3 min) |

## Initialization

```csharp
await client.InitializeAsync(maxRetries: 3);
```

Launches a headless Chromium browser, navigates to WhatsApp Web, injects scripts, and waits for authentication. Returns when the client is ready. Automatically retries on rate limit or session corruption.

After `InitializeAsync` completes, the following sub-client properties become available:

| Property | Type | Description |
|----------|------|-------------|
| `client.Chat` | `ChatService` | [Chat operations](chat.md) |
| `client.Message` | `MessageService` | [Message operations](message.md) |
| `client.Contact` | `ContactService` | [Contact operations](contact.md) |
| `client.Group` | `GroupService` | [Group operations](group.md) |
| `client.Profile` | `ProfileService` | [Profile operations](profile.md) |
| `client.Label` | `LabelService` | [Label operations](label.md) |

## Request Pairing Code

```csharp
string code = await client.RequestPairingCodeAsync(
    phoneNumber: "5511999999999",
    showNotification: true,
    intervalMs: 180000
);
```

Requests a pairing code for the given phone number. Automatically renews at the specified interval. Typically called from an `OnQr` handler when using pairing code authentication.

## Clear Session

```csharp
client.ClearSession();
```

Deletes session data from disk, forcing re-authentication on the next run.

## Dispose

`Client` implements `IAsyncDisposable`. Use `await using` or call `DisposeAsync()` to close the browser.

```csharp
await using var client = new Client();
// ... use client ...
// Browser is closed automatically when leaving scope
```

## See Also

- [Authentication](authentication.md) -- QR code and pairing code flows
- [Events](events.md) -- Lifecycle event handlers

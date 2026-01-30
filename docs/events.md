# Events

[Back to index](README.md)

All events support sync and async variants. Event methods return `Client` for fluent chaining.

## Event List

| Method | Callback | Description |
|--------|----------|-------------|
| `OnQr` / `OnQrAsync` | `Action<string>` / `Func<string, Task>` | QR code generated for authentication |
| `OnCode` / `OnCodeAsync` | `Action<string>` / `Func<string, Task>` | Pairing code received |
| `OnAuthenticated` / `OnAuthenticatedAsync` | `Action` / `Func<Task>` | Authentication completed |
| `OnReady` / `OnReadyAsync` | `Action` / `Func<Task>` | Client ready for use (sub-clients available) |
| `OnAuthStateChanged` / `OnAuthStateChangedAsync` | `Action<WaState>` / `Func<WaState, Task>` | Authentication state changed |
| `OnLoadingScreen` / `OnLoadingScreenAsync` | `Action<int>` / `Func<int, Task>` | Sync progress percentage |
| `OnDisconnected` / `OnDisconnectedAsync` | `Action<DisconnectReason>` / `Func<DisconnectReason, Task>` | Disconnected from WhatsApp |

## Fluent Chaining

```csharp
var client = new Client(options);

client
    .OnQr(qr => Console.WriteLine($"QR: {qr}"))
    .OnCode(code => Console.WriteLine($"Code: {code}"))
    .OnAuthenticated(() => Console.WriteLine("Authenticated!"))
    .OnReady(() => Console.WriteLine("Ready!"))
    .OnAuthStateChanged(state => Console.WriteLine($"State: {state}"))
    .OnLoadingScreen(percent => Console.WriteLine($"Loading: {percent}%"))
    .OnDisconnected(reason => Console.WriteLine($"Disconnected: {reason}"));

await client.InitializeAsync();
```

## Async Variant

Use async variants when the handler needs to perform I/O:

```csharp
client.OnReadyAsync(async () =>
{
    var chats = await client.Chat.GetChatsAsync();
    Console.WriteLine($"Loaded {chats.Count} chats");
});
```

## WaState Enum

Authentication states:

| Value | Description |
|-------|-------------|
| `Conflict` | Another session is active |
| `Connected` | Connected and authenticated |
| `DeprecatedVersion` | Client version is deprecated |
| `Opening` | Opening connection |
| `Pairing` | Pairing in progress |
| `ProxyBlock` | Blocked by proxy |
| `SmbTosBlock` | Blocked by SMB terms of service |
| `Timeout` | Connection timed out |
| `TosBlock` | Blocked by terms of service |
| `Unlaunched` | Not yet launched |
| `Unpaired` | Not paired |
| `UnpairedIdle` | Not paired (idle) |

## DisconnectReason Enum

| Value | Description |
|-------|-------------|
| `Logout` | User logged out |
| `MaxQrRetries` | Maximum QR code retries reached |
| `Conflict` | Another session took over |
| `DeprecatedVersion` | Client version is deprecated |
| `ProxyBlock` | Blocked by proxy |
| `SmbTosBlock` | Blocked by SMB terms of service |
| `Timeout` | Connection timed out |
| `TosBlock` | Blocked by terms of service |

## See Also

- [Client](client.md) -- Client configuration
- [Authentication](authentication.md) -- Authentication flows

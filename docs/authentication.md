# Authentication

[Back to index](README.md)

The client supports two authentication methods, configured via `ClientOptions`.

## QR Code (default)

Register the `OnQr` event to receive the QR code string.

```csharp
await using var client = new Client();

client.OnQr(qr => Console.WriteLine($"Scan this QR code: {qr}"));

await client.InitializeAsync();
```

## Phone Pairing Code

Set `PairWithPhoneNumber` in the options. The pairing code is emitted via the `OnCode` event.

```csharp
var options = new ClientOptions
{
    PairWithPhoneNumber = "5511999999999"
};

await using var client = new Client(options);

client.OnCode(code => Console.WriteLine($"Enter on phone: {code}"));

await client.InitializeAsync();
```

### Manual Pairing Code Request

You can also request a pairing code manually from within a QR handler:

```csharp
await using var client = new Client();

client.OnQrAsync(async _ =>
{
    var code = await client.RequestPairingCodeAsync("5511999999999");
    Console.WriteLine($"Enter on phone: {code}");
});

await client.InitializeAsync();
```

## Session Persistence

The session is saved automatically to `ClientOptions.SessionPath` (default: `./.wwebjs_auth/session`) and reused on subsequent runs without re-authentication.

To force re-authentication:

```csharp
client.ClearSession();
```

## Lifecycle

1. `InitializeAsync()` launches the browser and navigates to WhatsApp Web
2. If not authenticated, a QR code or pairing code is generated
3. User scans QR / enters pairing code on their phone
4. `OnAuthenticated` fires when auth completes
5. `OnReady` fires when the client is fully initialized and sub-clients are available

## See Also

- [Client](client.md) -- Client configuration
- [Events](events.md) -- All lifecycle events

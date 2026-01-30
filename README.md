# WhatsAppWeb.NET

[![NuGet](https://img.shields.io/nuget/v/WhatsAppWeb.NET.svg)](https://www.nuget.org/packages/WhatsAppWeb.NET)
[![NuGet Downloads](https://img.shields.io/nuget/dt/WhatsAppWeb.NET.svg)](https://www.nuget.org/packages/WhatsAppWeb.NET)
[![License](https://img.shields.io/github/license/alexandrereyes/WhatsAppWeb.NET)](LICENSE)

C# / .NET port of the [whatsapp-web.js](https://github.com/pedroslopez/whatsapp-web.js/) library (Node.js, by Pedro S. Lopez) for the .NET ecosystem.

The original project is a WhatsApp client library for Node.js that operates via WhatsApp Web using Puppeteer. This version reimplements the same approach in **C#**, replacing Puppeteer with **Microsoft Playwright** and exposing an idiomatic .NET API.

## Getting Started

### 1. Configure environment variables

The tests interact with the real WhatsApp Web and require phone numbers for authentication and operations.

```bash
cp .env.example .env
```

Edit `.env` with your numbers (international format without `+`, e.g.: `5511999998888`):

| Variable | Purpose |
|----------|---------|
| `PHONE_NUMBER` | Your phone number. Receives the pairing code |
| `TARGET_TEST_PHONE` | Target number for test operations (send messages, search chats, etc.) |

### 2. Build

```bash
dotnet build WhatsAppWeb.slnx -v q
```

### 3. Run the tests

```bash
dotnet test WhatsAppWeb.slnx --no-build
```

### 4. Pair with WhatsApp

On startup, the test runner displays a pairing code in the output:

```
[xUnit.net 00:00:10.23] WhatsAppWebTests: [Warning] ClientHooks: === PAIRING CODE: XXXX-XXXX ===
```

On your phone, go to **WhatsApp > Linked Devices > Link a Device > Enter code** and type in the code. You have **1 minute** before the timeout.

After pairing, the tests run automatically. The session is persisted on disk (`.wwebjs_auth/`), so subsequent runs will not require pairing again.

## Library usage

```csharp
using WhatsAppWebLib;

await using var client = new Client();

client.OnQrAsync(async _ =>
{
    var code = await client.RequestPairingCodeAsync("5511999999999");
    Console.WriteLine($"Enter on your phone: {code}");
});

client.OnReady(() => Console.WriteLine("Ready!"));

await client.InitializeAsync();

// Send message
var messageId = await client.Message.SendAsync("5511999999999", "Hello!");

// List chats
var chats = await client.Chat.GetChatsAsync();
```

For details on each feature, see the [full documentation](docs/).

## Features

| Feature | Status |
|---|---|
| Authentication via QR Code / Pairing Code | OK |
| Send text messages | OK |
| Reply, react, forward, edit, delete messages | OK |
| Star/unstar messages | OK |
| List and search chats | OK |
| Archive, pin, mute chats | OK |
| Mark chat as read | OK |
| Simulate typing | OK |
| Search messages | OK |
| List contacts, check registration | OK |
| Manage groups (participants, subject, description, invite) | OK |
| Profile picture (URL and Base64) | OK |
| Status and presence (online/offline) | OK |
| Labels (WhatsApp Business) | OK |
| Session persistence | OK |

## Useful commands

```bash
# Run a specific scenario
dotnet test WhatsAppWeb.slnx --no-build --filter "DisplayName~chat list"

# List all tests
dotnet test WhatsAppWeb.slnx --no-build --list-tests
```

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Pairing code does not appear | Check that `"diagnosticMessages": true` is set in `xunit.runner.json` |
| Authentication timeout | Enter the code on your phone within 1 minute |
| Tests hang without output | Always run `dotnet build` separately before `dotnet test --no-build` |

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 |
| Browser automation | Microsoft Playwright |
| Testing | xUnit v3 + ReqnRoll (Gherkin) + Shouldly |

## Disclaimer

This project is not affiliated, associated, authorized, endorsed, or in any way officially connected to WhatsApp or any of its subsidiaries or affiliates. There is no guarantee that you will not be blocked by using this method. WhatsApp does not allow bots or unofficial clients on its platform.

## License

Based on [whatsapp-web.js](https://github.com/pedroslopez/whatsapp-web.js/) by Pedro S. Lopez, licensed under the Apache License 2.0.

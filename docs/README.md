# Documentation

API reference for WhatsAppWeb.NET, a C#/.NET port of [whatsapp-web.js](https://github.com/pedroslopez/whatsapp-web.js/).

## Guides

| Document | Description |
|----------|-------------|
| [Client](client.md) | Client initialization, configuration, and session management |
| [Authentication](authentication.md) | QR code and phone pairing code authentication |
| [Events](events.md) | Lifecycle events (QR, ready, disconnected, etc.) |

## API Reference

| Document | Service | Description |
|----------|---------|-------------|
| [Message](message.md) | `client.Message` | Send, fetch, reply, react, forward, edit, delete, star, pin messages |
| [Chat](chat.md) | `client.Chat` | List, inspect, archive, pin, mute, search chats |
| [Contact](contact.md) | `client.Contact` | List, lookup, block/unblock contacts |
| [Group](group.md) | `client.Group` | Create groups, manage participants, settings, invites |
| [Profile](profile.md) | `client.Profile` | Profile pictures, status, display name, presence |
| [Label](label.md) | `client.Label` | Labels for WhatsApp Business accounts |

## Architecture

The library uses a sub-client pattern. After calling `client.InitializeAsync()`, functionality is accessed through service properties on the main `Client`:

```csharp
await using var client = new Client();
await client.InitializeAsync();

// Sub-clients are available after initialization
var chats = await client.Chat.GetChatsAsync();
var msgId = await client.Message.SendAsync("5511999999999", "Hello!");
```

## WhatsApp ID Format

WhatsApp uses suffixed IDs internally:

| Format | Example | Usage |
|--------|---------|-------|
| `{number}@c.us` | `5511999999999@c.us` | Individual contacts/chats |
| `{number}@g.us` | `120363012345678901@g.us` | Groups |

All methods auto-append `@c.us` if no suffix is provided, so you can pass just the phone number.

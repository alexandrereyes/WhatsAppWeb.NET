# Chat

[Back to index](README.md) | Related: [Message](message.md), [Group](group.md)

List, inspect, archive, pin, mute chats, and search messages via `client.Chat`.

## List Chats

```csharp
List<ChatModel> chats = await client.Chat.GetChatsAsync();
```

```csharp
Task<List<ChatModel>> GetChatsAsync()
```

## Get Chat by ID

```csharp
ChatModel? chat = await client.Chat.GetChatByIdAsync("5511999999999");

// Group
ChatModel? group = await client.Chat.GetChatByIdAsync("120363012345678901@g.us");
```

```csharp
Task<ChatModel?> GetChatByIdAsync(string chatId)
```

Auto-appends `@c.us` if no suffix is provided.

## Get Chat Info (detailed)

```csharp
ChatInfoModel? info = await client.Chat.GetChatInfoAsync("5511999999999");

if (info?.LastMessage != null)
    Console.WriteLine(info.LastMessage.Body);

if (info?.GroupMetadata != null)
    Console.WriteLine($"Owner: {info.GroupMetadata.Owner}");
```

```csharp
Task<ChatInfoModel?> GetChatInfoAsync(string chatId)
```

Returns extended chat data including the last message and group metadata (for groups).

## Mark as Read

```csharp
bool ok = await client.Chat.SendSeenAsync("5511999999999");
```

```csharp
Task<bool> SendSeenAsync(string chatId)
```

## Mark as Unread

```csharp
bool ok = await client.Chat.MarkUnreadAsync("5511999999999");
```

```csharp
Task<bool> MarkUnreadAsync(string chatId)
```

## Archive / Unarchive

```csharp
bool ok = await client.Chat.ArchiveAsync("5511999999999");
bool ok = await client.Chat.UnarchiveAsync("5511999999999");
```

```csharp
Task<bool> ArchiveAsync(string chatId)
Task<bool> UnarchiveAsync(string chatId)
```

## Pin / Unpin

```csharp
bool ok = await client.Chat.PinAsync("5511999999999");
bool ok = await client.Chat.UnpinAsync("5511999999999");
```

```csharp
Task<bool> PinAsync(string chatId)
Task<bool> UnpinAsync(string chatId)
```

WhatsApp allows a maximum of 3 pinned chats. `PinAsync` returns `false` if the limit is reached.

## Mute / Unmute

```csharp
// Mute indefinitely
MuteResult? result = await client.Chat.MuteAsync("5511999999999");

// Mute until a specific timestamp (Unix seconds)
MuteResult? result = await client.Chat.MuteAsync("5511999999999", unmuteTimestamp: 1700000000);

// Unmute
MuteResult? result = await client.Chat.UnmuteAsync("5511999999999");
```

```csharp
Task<MuteResult?> MuteAsync(string chatId, long? unmuteTimestamp = null)
Task<MuteResult?> UnmuteAsync(string chatId)
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `chatId` | `string` | | Chat ID or phone number |
| `unmuteTimestamp` | `long?` | `null` | Unix timestamp for auto-unmute (null = indefinite) |

## Clear Messages

```csharp
bool ok = await client.Chat.ClearMessagesAsync("5511999999999");
```

```csharp
Task<bool> ClearMessagesAsync(string chatId)
```

Clears all messages from the chat locally.

## Delete Chat

```csharp
bool ok = await client.Chat.DeleteAsync("5511999999999");
```

```csharp
Task<bool> DeleteAsync(string chatId)
```

## Chat State (Typing / Recording)

Simulate typing or recording indicators:

```csharp
// Show "typing..." indicator
await client.Chat.SendStateTypingAsync("5511999999999");

// Show "recording audio..." indicator
await client.Chat.SendStateRecordingAsync("5511999999999");

// Clear indicator
await client.Chat.ClearStateAsync("5511999999999");
```

```csharp
Task<bool> SendStateTypingAsync(string chatId)
Task<bool> SendStateRecordingAsync(string chatId)
Task<bool> ClearStateAsync(string chatId)
```

## Search Messages

```csharp
// Search across all chats
var results = await client.Chat.SearchMessagesAsync("search query");

// Search within a specific chat
var results = await client.Chat.SearchMessagesAsync("search query", chatId: "5511999999999");

// With pagination
var results = await client.Chat.SearchMessagesAsync("search query", pageNumber: 2, limit: 20);
```

```csharp
Task<List<MessageModel>> SearchMessagesAsync(string query, string? chatId = null, int? pageNumber = null, int? limit = null)
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `query` | `string` | | Search text |
| `chatId` | `string?` | `null` | Restrict search to a specific chat |
| `pageNumber` | `int?` | `1` | Page number for pagination |
| `limit` | `int?` | `10` | Results per page |

Returns `List<MessageModel>` (see [Message models](message.md#models)).

## Models

### ChatModel

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string?` | Chat ID (e.g., `5511999999999@c.us`) |
| `Name` | `string?` | Display name |
| `IsGroup` | `bool` | Whether this is a group chat |
| `IsReadOnly` | `bool` | Read-only chat |
| `UnreadCount` | `int` | Unread message count |
| `Timestamp` | `long?` | Last activity Unix timestamp |
| `Archived` | `bool` | Whether the chat is archived |
| `Pinned` | `bool` | Whether the chat is pinned |
| `IsMuted` | `bool` | Whether the chat is muted |
| `MuteExpiration` | `long?` | Mute expiration Unix timestamp |

### ChatInfoModel

Extends `ChatModel` with:

| Property | Type | Description |
|----------|------|-------------|
| `LastMessage` | `MessageModel?` | Last message in the chat |
| `GroupMetadata` | `GroupMetadata?` | Group metadata (groups only) |

### MuteResult

| Property | Type | Description |
|----------|------|-------------|
| `IsMuted` | `bool` | Current mute state |
| `MuteExpiration` | `long` | Mute expiration timestamp (0 = not muted) |

## See Also

- [Message](message.md) -- Message operations
- [Group](group.md) -- Group-specific operations

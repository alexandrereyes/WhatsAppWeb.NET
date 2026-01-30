# Message

[Back to index](README.md) | Related: [Chat](chat.md)

Send, fetch, reply, react, forward, edit, delete, star, and pin messages via `client.Message`.

## Send

```csharp
string? messageId = await client.Message.SendAsync("5511999999999", "Hello!");
```

```csharp
Task<string?> SendAsync(string chatId, string message)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `chatId` | `string` | Chat ID or phone number (`@c.us` auto-appended) |
| `message` | `string` | Text content |

Returns the serialized message ID, or `null` on error.

## Fetch Messages

```csharp
// All loaded messages
var messages = await client.Message.FetchAsync("5511999999999");

// With limit (auto-loads earlier messages)
var messages = await client.Message.FetchAsync("5511999999999", limit: 50);

// Only messages sent by me
var messages = await client.Message.FetchAsync("5511999999999", fromMe: true);
```

```csharp
Task<List<MessageModel>> FetchAsync(string chatId, int? limit = null, bool? fromMe = null)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `chatId` | `string` | Chat ID or phone number |
| `limit` | `int?` | Max messages to return (loads earlier messages as needed) |
| `fromMe` | `bool?` | Filter by sender (`true` = only mine, `false` = only others) |

## Get by ID

```csharp
MessageModel? message = await client.Message.GetByIdAsync("true_5511999999999@c.us_ABCDEF123456");
```

```csharp
Task<MessageModel?> GetByIdAsync(string messageId)
```

## Reply

```csharp
string? replyId = await client.Message.ReplyAsync("msgId", "Reply text");

// Reply in a different chat
string? replyId = await client.Message.ReplyAsync("msgId", "Reply text", chatId: "5511999999999");
```

```csharp
Task<string?> ReplyAsync(string messageId, string content, string? chatId = null)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `messageId` | `string` | ID of the message to reply to |
| `content` | `string` | Reply text |
| `chatId` | `string?` | Target chat (defaults to original message's chat) |

## React

```csharp
bool ok = await client.Message.ReactAsync("msgId", "\ud83d\udc4d");

// Remove reaction
bool ok = await client.Message.ReactAsync("msgId", "");
```

```csharp
Task<bool> ReactAsync(string messageId, string reaction)
```

## Forward

```csharp
bool ok = await client.Message.ForwardAsync("msgId", "5511999999999");
```

```csharp
Task<bool> ForwardAsync(string messageId, string chatId)
```

## Edit

```csharp
bool ok = await client.Message.EditAsync("msgId", "Updated text");
```

```csharp
Task<bool> EditAsync(string messageId, string content)
```

Only works on messages you sent and that are within the edit time window.

## Delete

```csharp
// Delete for me only
bool ok = await client.Message.DeleteAsync("msgId");

// Delete for everyone
bool ok = await client.Message.DeleteAsync("msgId", everyone: true);

// Delete for everyone, keep media
bool ok = await client.Message.DeleteAsync("msgId", everyone: true, clearMedia: false);
```

```csharp
Task<bool> DeleteAsync(string messageId, bool everyone = false, bool clearMedia = true)
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `messageId` | `string` | | Message ID |
| `everyone` | `bool` | `false` | Delete for all participants |
| `clearMedia` | `bool` | `true` | Also clear associated media |

## Star / Unstar

```csharp
bool ok = await client.Message.StarAsync("msgId");
bool ok = await client.Message.UnstarAsync("msgId");
```

```csharp
Task<bool> StarAsync(string messageId)
Task<bool> UnstarAsync(string messageId)
```

## Pin / Unpin

```csharp
bool ok = await client.Message.PinAsync("msgId", durationSeconds: 604800); // 7 days
bool ok = await client.Message.UnpinAsync("msgId");
```

```csharp
Task<bool> PinAsync(string messageId, int durationSeconds)
Task<bool> UnpinAsync(string messageId)
```

## Get Message Info

```csharp
MessageInfoModel? info = await client.Message.GetInfoAsync("msgId");
```

```csharp
Task<MessageInfoModel?> GetInfoAsync(string messageId)
```

Returns delivery/read/played status. Only works for messages sent by you.

## Get Reactions

```csharp
string? reactionsJson = await client.Message.GetReactionsAsync("msgId");
```

```csharp
Task<string?> GetReactionsAsync(string messageId)
```

Returns a JSON string with reaction data, or `null` if no reactions exist.

## Models

### MessageModel

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string?` | Serialized message ID |
| `Body` | `string?` | Message content |
| `From` | `string?` | Sender ID |
| `To` | `string?` | Recipient ID |
| `FromMe` | `bool` | Whether sent by the authenticated user |
| `Timestamp` | `long?` | Unix timestamp |
| `Type` | `string?` | Message type (`chat`, `image`, etc.) |
| `HasMedia` | `bool` | Whether the message contains media |

### MessageInfoModel

| Property | Type | Description |
|----------|------|-------------|
| `Delivery` | `object?` | Delivery status info |
| `Read` | `object?` | Read receipt info |
| `Played` | `object?` | Played status (for audio/video) |

## See Also

- [Chat](chat.md) -- Chat operations including message search

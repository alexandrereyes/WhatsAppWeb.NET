# Label

[Back to index](README.md) | Related: [Chat](chat.md)

Labels for WhatsApp Business accounts via `client.Label`.

> Labels are a WhatsApp Business feature. These methods require a Business account.

## List Labels

```csharp
List<LabelModel> labels = await client.Label.GetLabelsAsync();

foreach (var label in labels)
    Console.WriteLine($"{label.Name} ({label.HexColor})");
```

```csharp
Task<List<LabelModel>> GetLabelsAsync()
```

## Get Label by ID

```csharp
LabelModel? label = await client.Label.GetLabelByIdAsync("1");
```

```csharp
Task<LabelModel?> GetLabelByIdAsync(string labelId)
```

## Get Labels for a Chat

```csharp
List<LabelModel> labels = await client.Label.GetChatLabelsAsync("5511999999999");
```

```csharp
Task<List<LabelModel>> GetChatLabelsAsync(string chatId)
```

## Get Chats by Label

```csharp
List<string> chatIds = await client.Label.GetChatsByLabelIdAsync("1");
```

```csharp
Task<List<string>> GetChatsByLabelIdAsync(string labelId)
```

Returns a list of chat IDs that have the given label.

## Add or Remove Labels

```csharp
bool ok = await client.Label.AddOrRemoveLabelsAsync(
    labelIds: new[] { 1, 2 },
    chatIds: new[] { "5511999999999@c.us" }
);
```

```csharp
Task<bool> AddOrRemoveLabelsAsync(int[] labelIds, string[] chatIds)
```

Applies or removes the specified labels to/from the given chats.

## Models

### LabelModel

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string?` | Label ID |
| `Name` | `string?` | Label name |
| `HexColor` | `string?` | Label color in hex format |

## See Also

- [Chat](chat.md) -- Chat operations

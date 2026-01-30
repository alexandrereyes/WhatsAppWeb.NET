# Group

[Back to index](README.md) | Related: [Chat](chat.md)

Create groups, manage participants, update settings, and handle invites via `client.Group`.

## Get Participants

```csharp
List<GroupParticipant>? participants = await client.Group.GetParticipantsAsync("120363012345678901@g.us");

if (participants != null)
{
    foreach (var p in participants)
        Console.WriteLine($"{p.Id} - Admin: {p.IsAdmin}");
}
```

```csharp
Task<List<GroupParticipant>?> GetParticipantsAsync(string groupId)
```

Returns `null` if the chat is not a group or has no metadata.

## Create Group

```csharp
string? groupId = await client.Group.CreateGroupAsync(
    "Group Name",
    new[] { "5511999999999@c.us", "5511888888888@c.us" }
);
```

```csharp
Task<string?> CreateGroupAsync(string title, string[] participantIds)
```

Returns the new group ID, or `null` on error.

## Add Participants

```csharp
bool ok = await client.Group.AddParticipantsAsync(
    "120363012345678901@g.us",
    new[] { "5511999999999@c.us" }
);
```

```csharp
Task<bool> AddParticipantsAsync(string groupId, string[] participantIds)
```

## Remove Participants

```csharp
bool ok = await client.Group.RemoveParticipantsAsync(
    "120363012345678901@g.us",
    new[] { "5511999999999@c.us" }
);
```

```csharp
Task<bool> RemoveParticipantsAsync(string groupId, string[] participantIds)
```

## Promote to Admin

```csharp
bool ok = await client.Group.PromoteParticipantsAsync(
    "120363012345678901@g.us",
    new[] { "5511999999999@c.us" }
);
```

```csharp
Task<bool> PromoteParticipantsAsync(string groupId, string[] participantIds)
```

## Demote from Admin

```csharp
bool ok = await client.Group.DemoteParticipantsAsync(
    "120363012345678901@g.us",
    new[] { "5511999999999@c.us" }
);
```

```csharp
Task<bool> DemoteParticipantsAsync(string groupId, string[] participantIds)
```

## Set Subject

```csharp
bool ok = await client.Group.SetSubjectAsync("120363012345678901@g.us", "New Group Name");
```

```csharp
Task<bool> SetSubjectAsync(string groupId, string subject)
```

## Set Description

```csharp
bool ok = await client.Group.SetDescriptionAsync("120363012345678901@g.us", "New description");
```

```csharp
Task<bool> SetDescriptionAsync(string groupId, string description)
```

## Set Messages Admins Only

```csharp
// Only admins can send messages
bool ok = await client.Group.SetMessagesAdminsOnlyAsync("120363012345678901@g.us", true);

// Everyone can send messages
bool ok = await client.Group.SetMessagesAdminsOnlyAsync("120363012345678901@g.us", false);
```

```csharp
Task<bool> SetMessagesAdminsOnlyAsync(string groupId, bool adminsOnly = true)
```

## Set Info Admins Only

```csharp
// Only admins can edit group info
bool ok = await client.Group.SetInfoAdminsOnlyAsync("120363012345678901@g.us", true);

// Everyone can edit group info
bool ok = await client.Group.SetInfoAdminsOnlyAsync("120363012345678901@g.us", false);
```

```csharp
Task<bool> SetInfoAdminsOnlyAsync(string groupId, bool adminsOnly = true)
```

## Get Invite Code

```csharp
string? code = await client.Group.GetInviteCodeAsync("120363012345678901@g.us");
```

```csharp
Task<string?> GetInviteCodeAsync(string groupId)
```

## Revoke Invite

```csharp
string? newCode = await client.Group.RevokeInviteAsync("120363012345678901@g.us");
```

```csharp
Task<string?> RevokeInviteAsync(string groupId)
```

Returns the new invite code after revocation.

## Get Invite Info

```csharp
string? infoJson = await client.Group.GetInviteInfoAsync("ABCdEfGHIjk");
```

```csharp
Task<string?> GetInviteInfoAsync(string inviteCode)
```

Returns group info as a JSON string for the given invite code.

## Accept Invite

```csharp
string? groupId = await client.Group.AcceptInviteAsync("ABCdEfGHIjk");
```

```csharp
Task<string?> AcceptInviteAsync(string inviteCode)
```

Returns the group ID on success.

## Leave Group

```csharp
bool ok = await client.Group.LeaveAsync("120363012345678901@g.us");
```

```csharp
Task<bool> LeaveAsync(string groupId)
```

## Models

### GroupParticipant

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string?` | Participant ID |
| `IsAdmin` | `bool` | Whether the participant is an admin |
| `IsSuperAdmin` | `bool` | Whether the participant is a super admin (group creator) |

### GroupMetadata

Returned as part of `ChatInfoModel` (see [Chat](chat.md)).

| Property | Type | Description |
|----------|------|-------------|
| `Owner` | `string?` | Group owner ID |
| `Creation` | `long?` | Creation Unix timestamp |
| `Description` | `string?` | Group description |
| `Participants` | `List<GroupParticipant>?` | List of participants |

## See Also

- [Chat](chat.md) -- Chat operations including `GetChatInfoAsync` for group metadata

# Contact

[Back to index](README.md) | Related: [Chat](chat.md), [Profile](profile.md)

List, lookup, verify, and block/unblock contacts via `client.Contact`.

## List Contacts

```csharp
List<ContactModel> contacts = await client.Contact.GetContactsAsync();
```

```csharp
Task<List<ContactModel>> GetContactsAsync()
```

## Get Contact by ID

```csharp
ContactModel? contact = await client.Contact.GetContactByIdAsync("5511999999999");
```

```csharp
Task<ContactModel?> GetContactByIdAsync(string contactId)
```

## Get Number ID

Verify a phone number and get its WhatsApp ID:

```csharp
NumberIdResult? result = await client.Contact.GetNumberIdAsync("5511999999999");

if (result != null)
{
    Console.WriteLine($"WID: {result.Wid}");
    Console.WriteLine($"Business: {result.IsBusiness}");
    Console.WriteLine($"Verified name: {result.VerifiedName}");
}
```

```csharp
Task<NumberIdResult?> GetNumberIdAsync(string number)
```

Returns `null` if the number is not registered on WhatsApp.

## Check Registration

```csharp
bool isRegistered = await client.Contact.IsRegisteredUserAsync("5511999999999");
```

```csharp
Task<bool> IsRegisteredUserAsync(string contactId)
```

## Get Blocked Contacts

```csharp
List<ContactModel> blocked = await client.Contact.GetBlockedContactsAsync();
```

```csharp
Task<List<ContactModel>> GetBlockedContactsAsync()
```

## Block / Unblock

```csharp
bool ok = await client.Contact.BlockAsync("5511999999999");
bool ok = await client.Contact.UnblockAsync("5511999999999");
```

```csharp
Task<bool> BlockAsync(string contactId)
Task<bool> UnblockAsync(string contactId)
```

## Get About (Status Text)

```csharp
string? about = await client.Contact.GetAboutAsync("5511999999999");
```

```csharp
Task<string?> GetAboutAsync(string contactId)
```

Returns the contact's "About" status text, or `null` if unavailable.

## Get Common Groups

```csharp
List<string> groupIds = await client.Contact.GetCommonGroupsAsync("5511999999999");
```

```csharp
Task<List<string>> GetCommonGroupsAsync(string contactId)
```

Returns a list of group IDs that you share with the given contact.

## Models

### ContactModel

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string?` | Contact ID (e.g., `5511999999999@c.us`) |
| `Number` | `string?` | Phone number |
| `Name` | `string?` | Contact name (from address book) |
| `Pushname` | `string?` | Name set by the user on their profile |
| `ShortName` | `string?` | Short name |
| `IsMe` | `bool` | Whether this is the authenticated user |
| `IsUser` | `bool` | Whether this is an individual user |
| `IsGroup` | `bool` | Whether this is a group |
| `IsWAContact` | `bool` | Whether this is a WhatsApp contact |
| `IsMyContact` | `bool` | Whether this is in your address book |
| `IsBusiness` | `bool` | Whether this is a business account |
| `IsBlocked` | `bool` | Whether this contact is blocked |

### NumberIdResult

| Property | Type | Description |
|----------|------|-------------|
| `Wid` | `string?` | WhatsApp ID |
| `IsBusiness` | `bool` | Whether it's a business account |
| `VerifiedName` | `string?` | Verified business name |

## See Also

- [Profile](profile.md) -- Profile pictures and presence
- [Chat](chat.md) -- Chat operations

# Profile

[Back to index](README.md) | Related: [Contact](contact.md)

Profile pictures, status, display name, and presence via `client.Profile`.

## Get Profile Picture URL

```csharp
string? url = await client.Profile.GetPicUrlAsync("5511999999999");
```

```csharp
Task<string?> GetPicUrlAsync(string contactId)
```

Returns a temporary URL to the profile picture, or `null` if unavailable. Works with both contact IDs (`@c.us`) and group IDs (`@g.us`).

## Get Profile Picture as Base64

```csharp
string? base64 = await client.Profile.GetPicBase64Async("5511999999999");
```

```csharp
Task<string?> GetPicBase64Async(string contactId)
```

Downloads the profile picture and returns it as a Base64-encoded string.

## Delete Profile Picture

```csharp
bool ok = await client.Profile.DeleteProfilePictureAsync();
```

```csharp
Task<bool> DeleteProfilePictureAsync()
```

Removes the authenticated user's profile picture.

## Set Status (About Text)

```csharp
bool ok = await client.Profile.SetStatusAsync("Available");
```

```csharp
Task<bool> SetStatusAsync(string status)
```

Sets the authenticated user's "About" status text.

## Set Display Name

```csharp
bool ok = await client.Profile.SetDisplayNameAsync("My Name");
```

```csharp
Task<bool> SetDisplayNameAsync(string displayName)
```

Sets the authenticated user's push name (display name visible to others).

## Presence

Control your online/offline presence:

```csharp
// Appear online
bool ok = await client.Profile.SendPresenceAvailableAsync();

// Appear offline
bool ok = await client.Profile.SendPresenceUnavailableAsync();
```

```csharp
Task<bool> SendPresenceAvailableAsync()
Task<bool> SendPresenceUnavailableAsync()
```

## See Also

- [Contact](contact.md) -- Contact lookup and `GetAboutAsync`

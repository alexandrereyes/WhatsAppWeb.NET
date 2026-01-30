using JetBrains.Annotations;

namespace WhatsAppWebLib.Contact;

[UsedImplicitly]
public class NumberIdResult
{
    public string? Wid { get; set; }
    public bool IsBusiness { get; set; }
    public string? VerifiedName { get; set; }
}

[UsedImplicitly]
public class ContactModel
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public string? Name { get; init; }
    public string? Pushname { get; init; }
    public string? ShortName { get; init; }
    public bool IsMe { get; init; }
    public bool IsUser { get; init; }
    public bool IsGroup { get; init; }
    public bool IsWAContact { get; init; }
    public bool IsMyContact { get; init; }
    public bool IsBusiness { get; init; }
    public bool IsBlocked { get; init; }
}

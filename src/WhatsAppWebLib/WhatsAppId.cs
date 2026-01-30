namespace WhatsAppWebLib;

/// <summary>
/// Normalizes WhatsApp IDs by ensuring the correct suffix (@c.us, @g.us, etc.).
/// WhatsApp uses suffixed IDs internally:
/// - @c.us for individual contacts/chats
/// - @g.us for groups
/// </summary>
public static class WhatsAppId
{
    private const string ContactSuffix = "@c.us";
    private const string GroupSuffix = "@g.us";

    /// <summary>
    /// Ensures the ID has a valid chat suffix (@c.us or @g.us).
    /// If missing, defaults to @c.us.
    /// </summary>
    public static string EnsureChatSuffix(string id)
    {
        if (id.EndsWith(ContactSuffix) || id.EndsWith(GroupSuffix))
            return id;

        return id + ContactSuffix;
    }

    /// <summary>
    /// Ensures the ID has the @g.us suffix, replacing @c.us if present.
    /// </summary>
    public static string EnsureGroupSuffix(string id)
    {
        if (id.EndsWith(GroupSuffix))
            return id;

        return id.Replace(ContactSuffix, "") + GroupSuffix;
    }
}

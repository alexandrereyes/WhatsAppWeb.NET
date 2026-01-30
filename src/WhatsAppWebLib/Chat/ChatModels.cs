using WhatsAppWebLib.Group;
using WhatsAppWebLib.Message;

namespace WhatsAppWebLib.Chat;

public class ChatModel
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public bool IsGroup { get; init; }
    public bool IsReadOnly { get; init; }
    public int UnreadCount { get; init; }
    public long? Timestamp { get; init; }
    public bool Archived { get; init; }
    public bool Pinned { get; init; }
    public bool IsMuted { get; init; }
    public long? MuteExpiration { get; init; }
}

public class ChatInfoModel : ChatModel
{
    public MessageModel? LastMessage { get; init; }
    public GroupMetadata? GroupMetadata { get; init; }
}

public class MuteResult
{
    public bool IsMuted { get; init; }
    public long MuteExpiration { get; init; }
}

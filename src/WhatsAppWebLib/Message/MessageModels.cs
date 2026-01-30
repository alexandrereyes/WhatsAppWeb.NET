namespace WhatsAppWebLib.Message;

public class MessageModel
{
    public string? Id { get; init; }
    public string? Body { get; init; }
    public string? From { get; init; }
    public string? To { get; init; }
    public bool FromMe { get; init; }
    public long? Timestamp { get; init; }
    public string? Type { get; init; }
    public bool HasMedia { get; init; }
}

public class MessageInfoModel
{
    public object? Delivery { get; init; }
    public object? Read { get; init; }
    public object? Played { get; init; }
}

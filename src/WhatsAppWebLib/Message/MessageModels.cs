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

/// <summary>
/// Options for sending a message via WhatsApp.
/// Port of the upstream MessageSendOptions typedef.
/// </summary>
/// <seealso href="https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L949-L981"/>
public class MessageSendOptions
{
    /// <summary>Show link preview (default: true).</summary>
    public bool LinkPreview { get; init; } = true;

    /// <summary>Mark conversation as seen before sending (default: true).</summary>
    public bool SendSeen { get; init; } = true;

    /// <summary>Send audio as voice message with waveform.</summary>
    public bool SendAudioAsVoice { get; init; }

    /// <summary>Send video as GIF.</summary>
    public bool SendVideoAsGif { get; init; }

    /// <summary>Send media as sticker.</summary>
    public bool SendMediaAsSticker { get; init; }

    /// <summary>Send media as document attachment.</summary>
    public bool SendMediaAsDocument { get; init; }

    /// <summary>Send image as HD quality.</summary>
    public bool SendMediaAsHd { get; init; }

    /// <summary>Send as view-once message.</summary>
    public bool IsViewOnce { get; init; }

    /// <summary>Caption for image/video/document messages.</summary>
    public string? Caption { get; init; }

    /// <summary>Message ID to quote/reply to.</summary>
    public string? QuotedMessageId { get; init; }

    /// <summary>Parse vCards automatically (default: true).</summary>
    public bool ParseVCards { get; init; } = true;

    /// <summary>
    /// Media to attach. When provided via this property, the message content
    /// becomes the caption text.
    /// </summary>
    public MessageMedia? Media { get; init; }

    /// <summary>User IDs to mention in the message.</summary>
    public List<string>? Mentions { get; init; }

    /// <summary>Sticker author metadata.</summary>
    public string? StickerAuthor { get; init; }

    /// <summary>Sticker name metadata.</summary>
    public string? StickerName { get; init; }

    /// <summary>Sticker emoji categories.</summary>
    public List<string>? StickerCategories { get; init; }

    /// <summary>Extra options passed through to the underlying send function.</summary>
    public Dictionary<string, object>? ExtraOptions { get; init; }

    /// <summary>
    /// Pre-generated waveform for voice messages (PTT).
    /// 64-byte array with values 0-100, generated server-side via ffmpeg.
    /// When set, the browser skips Web Audio API waveform generation (which fails in headless Chromium).
    /// </summary>
    internal byte[]? PreGeneratedWaveform { get; init; }
}

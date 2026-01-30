using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace WhatsAppWebLib.Message;

public class MessageService(IPage page, JsonSerializerOptions jsonOptions, ILogger logger, string? ffmpegPath = null)
{
    private readonly MediaPreprocessor _preprocessor = new(logger, ffmpegPath ?? "ffmpeg");
    // @wwebjs-source WWebJS.sendMessage -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L26-L369
    // @wwebjs-source Client.sendMessage -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L983-L1129
    // @wwebjs-source WWebJS.getChat -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L568-L590

    /// <summary>
    /// Sends a text message to the specified chat.
    /// </summary>
    public Task<string?> SendAsync(string chatId, string content)
        => SendCoreAsync(chatId, content, null);

    /// <summary>
    /// Sends a media message to the specified chat.
    /// When <paramref name="content"/> is provided, it becomes the caption.
    /// </summary>
    public Task<string?> SendAsync(string chatId, MessageMedia media, string? content = null)
        => SendCoreAsync(chatId, content ?? string.Empty, new MessageSendOptions { Media = media });

    /// <summary>
    /// Sends a message with full options (media, caption, reply, mentions, etc.).
    /// When <see cref="MessageSendOptions.Media"/> is set, the <paramref name="content"/> becomes the caption.
    /// </summary>
    public Task<string?> SendAsync(string chatId, string content, MessageSendOptions options)
        => SendCoreAsync(chatId, content, options);

    // @wwebjs-source Client.sendMessage -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L983-L1129
    // @wwebjs-source WWebJS.sendMessage -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L26-L369
    // @wwebjs-source WWebJS.processMediaData -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L462-L536
    // @wwebjs-source WWebJS.mediaInfoToFile -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L1184-L1195
    private async Task<string?> SendCoreAsync(string chatId, string content, MessageSendOptions? options)
    {
        chatId = WhatsAppId.EnsureChatSuffix(chatId);

        try
        {
            // Server-side preprocessing via ffmpeg (before passing to browser)
            options = await PreprocessMediaAsync(options);

            var internalOptions = BuildInternalOptions(content, options, out var effectiveContent);

            return await page.EvaluateAsync<string?>(
                """
                async (args) => {
                    const { chatId, content, options, sendSeen } = args;
                    const chat = await window.WWebJS.getChat(chatId, { getAsModel: false });
                    if (!chat) return null;

                    if (sendSeen) {
                        await window.WWebJS.sendSeen(chatId);
                    }

                    // When a pre-generated waveform is provided (from server-side ffmpeg),
                    // temporarily override generateWaveform so the browser doesn't need
                    // Web Audio API (which fails in headless Chromium).
                    const originalGenerateWaveform = window.WWebJS.generateWaveform;
                    if (options.pttWaveform) {
                        window.WWebJS.generateWaveform = async () => new Uint8Array(options.pttWaveform);
                    }

                    try {
                        const msg = await window.WWebJS.sendMessage(chat, content, options);
                        return msg?.id?._serialized || msg?.id?.id || null;
                    } catch (e) {
                        throw new Error('SendMessage failed: ' + (e?.message || e?.toString?.() || JSON.stringify(e)));
                    } finally {
                        window.WWebJS.generateWaveform = originalGenerateWaveform;
                    }
                }
                """, new
                {
                    chatId,
                    content = effectiveContent,
                    options = internalOptions,
                    sendSeen = options?.SendSeen ?? true
                });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending message to {ChatId}", chatId);
            return null;
        }
    }

    /// <summary>
    /// Server-side media preprocessing using ffmpeg.
    /// Handles two cases that cannot be done browser-side in headless Chromium:
    /// 1. Voice messages (PTT): pre-generates the 64-sample waveform
    /// 2. Video stickers: converts video to animated WebP + injects EXIF metadata
    /// </summary>
    // @wwebjs-source Client.sendMessage sticker preprocessing -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1073-L1081
    // @wwebjs-source Util.formatToWebpSticker -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Util.js#L80-L190
    // @wwebjs-source WWebJS.generateWaveform -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L1145-L1182
    private async Task<MessageSendOptions?> PreprocessMediaAsync(MessageSendOptions? options)
    {
        if (options?.Media is null) return options;

        var media = options.Media;
        var modified = false;
        byte[]? waveform = null;

        // Voice/PTT: convert audio to OGG/Opus (required by WhatsApp) and pre-generate waveform
        if (options.SendAudioAsVoice)
        {
            // WhatsApp PTT requires OGG/Opus; convert if not already in that format
            if (!media.MimeType.Contains("opus", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogDebug("Converting audio to OGG/Opus for voice message");
                var opusMedia = await _preprocessor.ConvertToOggOpusAsync(media);
                if (opusMedia is not null)
                {
                    media = opusMedia;
                    modified = true;
                    logger.LogDebug("Audio converted to OGG/Opus ({Size} bytes)", media.FileSize);
                }
                else
                {
                    logger.LogWarning("Audio to OGG/Opus conversion failed, sending original format");
                }
            }

            logger.LogDebug("Pre-generating waveform for voice message");
            var audioBytes = Convert.FromBase64String(media.Data);
            waveform = await _preprocessor.GenerateWaveformAsync(audioBytes);
            if (waveform is not null)
                logger.LogDebug("Waveform generated: {Length} samples", waveform.Length);
            else
                logger.LogWarning("Waveform generation failed, browser will attempt fallback");
        }

        // Video sticker: convert video to animated WebP + inject EXIF
        if (options.SendMediaAsSticker && media.MimeType.Contains("video", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug("Converting video to WebP sticker");
            var webpMedia = await _preprocessor.ConvertVideoToWebpStickerAsync(media);
            if (webpMedia is not null)
            {
                // Inject sticker EXIF metadata if name/author provided
                if (!string.IsNullOrEmpty(options.StickerName) || !string.IsNullOrEmpty(options.StickerAuthor))
                {
                    var webpBytes = Convert.FromBase64String(webpMedia.Data);
                    var exifBytes = MediaPreprocessor.InjectStickerExif(
                        webpBytes, options.StickerName, options.StickerAuthor, options.StickerCategories);
                    webpMedia = new MessageMedia
                    {
                        MimeType = webpMedia.MimeType,
                        Data = Convert.ToBase64String(exifBytes),
                        FileName = webpMedia.FileName,
                        FileSize = exifBytes.Length
                    };
                }

                media = webpMedia;
                modified = true;
                logger.LogDebug("Video converted to WebP sticker ({Size} bytes)", media.FileSize);
            }
            else
            {
                logger.LogWarning("Video to WebP sticker conversion failed");
            }
        }

        // Image sticker: EXIF injection only (browser handles image→webp via StickerTools)
        // The browser-side toStickerData converts the image, but EXIF must be injected
        // after conversion. For images, we pass the EXIF metadata and let the JS handle it.
        // Note: For image stickers, EXIF is injected in the JS side after toStickerData converts to webp.

        if (!modified && waveform is null) return options;

        // Return new options with preprocessed media and/or waveform
        return new MessageSendOptions
        {
            LinkPreview = options.LinkPreview,
            SendSeen = options.SendSeen,
            SendAudioAsVoice = options.SendAudioAsVoice,
            SendVideoAsGif = options.SendVideoAsGif,
            SendMediaAsSticker = options.SendMediaAsSticker,
            SendMediaAsDocument = options.SendMediaAsDocument,
            SendMediaAsHd = options.SendMediaAsHd,
            IsViewOnce = options.IsViewOnce,
            Caption = options.Caption,
            QuotedMessageId = options.QuotedMessageId,
            ParseVCards = options.ParseVCards,
            Media = modified ? media : options.Media,
            Mentions = options.Mentions,
            StickerAuthor = options.StickerAuthor,
            StickerName = options.StickerName,
            StickerCategories = options.StickerCategories,
            ExtraOptions = options.ExtraOptions,
            PreGeneratedWaveform = waveform
        };
    }

    /// <summary>
    /// Builds the internal options object that gets passed to window.WWebJS.sendMessage().
    /// Follows the upstream Client.sendMessage logic for mapping MessageSendOptions to internal format.
    /// </summary>
    // @wwebjs-source Client.sendMessage options mapping -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1004-L1065
    private static Dictionary<string, object?> BuildInternalOptions(
        string content,
        MessageSendOptions? options,
        out string effectiveContent)
    {
        effectiveContent = content;
        var result = new Dictionary<string, object?>();

        if (options is null) return result;

        // Link preview
        if (!options.LinkPreview)
            result["linkPreview"] = false;

        // Media-related flags
        if (options.SendAudioAsVoice)
            result["sendAudioAsVoice"] = true;

        if (options.SendVideoAsGif)
            result["sendVideoAsGif"] = true;

        if (options.SendMediaAsSticker)
            result["sendMediaAsSticker"] = true;

        if (options.SendMediaAsDocument)
            result["sendMediaAsDocument"] = true;

        if (options.SendMediaAsHd)
            result["sendMediaAsHd"] = true;

        if (options.IsViewOnce)
            result["isViewOnce"] = true;

        // Caption
        if (options.Caption is not null)
            result["caption"] = options.Caption;

        // Quoted message (reply)
        if (options.QuotedMessageId is not null)
            result["quotedMessageId"] = options.QuotedMessageId;

        // Parse vCards
        if (!options.ParseVCards)
            result["parseVCards"] = false;

        // Mentions
        if (options.Mentions is { Count: > 0 })
            result["mentionedJidList"] = options.Mentions;

        // Extra options
        if (options.ExtraOptions is { Count: > 0 })
            result["extraOptions"] = options.ExtraOptions;

        // Media attachment - when media is present, content becomes empty and
        // the media data is passed inside options for the injected JS to process
        if (options.Media is not null)
        {
            result["media"] = new
            {
                mimetype = options.Media.MimeType,
                data = options.Media.Data,
                filename = options.Media.FileName,
                filesize = options.Media.FileSize
            };

            // When media is provided via options, the original content becomes the caption
            if (!string.IsNullOrEmpty(content))
                result["caption"] = content;

            effectiveContent = string.Empty;
        }

        // Pre-generated waveform for voice messages (server-side via ffmpeg)
        // Passed as an int array (0-100) so the JS side can set it on mediaData.waveform
        if (options.PreGeneratedWaveform is { Length: > 0 })
            result["pttWaveform"] = options.PreGeneratedWaveform.Select(b => (int)b).ToArray();

        return result;
    }

    // @wwebjs-source Chat.fetchMessages -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/Chat.js#L192-L223
    // @wwebjs-source Store.ConversationMsgs -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Store.js#L84
    // @wwebjs-source WWebJS.getMessageModel -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L539-L566
    public async Task<List<MessageModel>> FetchAsync(string chatId, int? limit = null, bool? fromMe = null)
    {
        chatId = WhatsAppId.EnsureChatSuffix(chatId);

        try
        {
            var jsonStr = await page.EvaluateAsync<string>(
                """
                async (args) => {
                    const { chatId, limit, fromMe } = args;
                    const msgFilter = (m) => {
                        if (m.isNotification) return false;
                        if (fromMe !== undefined && fromMe !== null && m.id.fromMe !== fromMe) return false;
                        return true;
                    };

                    const chat = await window.WWebJS.getChat(chatId, { getAsModel: false });
                    if (!chat) return '[]';

                    let msgs = chat.msgs.getModelsArray().filter(msgFilter);

                    if (limit && limit > 0) {
                        while (msgs.length < limit) {
                            const loaded = await window.Store.ConversationMsgs.loadEarlierMsgs(chat);
                            if (!loaded || !loaded.length) break;
                            msgs = [...loaded.filter(msgFilter), ...msgs];
                        }

                        if (msgs.length > limit) {
                            msgs.sort((a, b) => (a.t > b.t) ? 1 : -1);
                            msgs = msgs.splice(msgs.length - limit);
                        }
                    }

                    return JSON.stringify(msgs.map(m => ({
                        id: m.id?._serialized ?? m.id,
                        body: m.body,
                        from: m.from?._serialized ?? m.from,
                        to: m.to?._serialized ?? m.to,
                        fromMe: m.id?.fromMe ?? false,
                        timestamp: m.t,
                        type: m.type,
                        hasMedia: m.hasMedia ?? false
                    })));
                }
                """, new { chatId, limit = limit ?? -1, fromMe });
            return JsonSerializer.Deserialize<List<MessageModel>>(jsonStr, jsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching messages: {ChatId}", chatId);
            return [];
        }
    }

    // @wwebjs-source Client.getMessageById -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1247-L1263
    // @wwebjs-source WWebJS.getMessageModel -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L539-L566
    public async Task<MessageModel?> GetByIdAsync(string messageId)
    {
        try
        {
            var jsonStr = await page.EvaluateAsync<string?>(
                """
                async (messageId) => {
                    let msg = window.Store.Msg.get(messageId);

                    if (!msg) {
                        const params = messageId.split('_');
                        if (params.length !== 3 && params.length !== 4) return null;

                        const messagesObject = await window.Store.Msg.getMessagesById([messageId]);
                        if (messagesObject?.messages?.length) {
                            msg = messagesObject.messages[0];
                        }
                    }

                    if (!msg) return null;

                    return JSON.stringify({
                        id: msg.id?._serialized ?? msg.id,
                        body: msg.body,
                        from: msg.from?._serialized ?? msg.from,
                        to: msg.to?._serialized ?? msg.to,
                        fromMe: msg.id?.fromMe ?? false,
                        timestamp: msg.t,
                        type: msg.type,
                        hasMedia: msg.hasMedia ?? false
                    });
                }
                """, messageId);
            return string.IsNullOrEmpty(jsonStr) ? null : JsonSerializer.Deserialize<MessageModel>(jsonStr, jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting message: {MessageId}", messageId);
            return null;
        }
    }

    // @wwebjs-source Message.delete -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/Message.js#L509-L526
    // @wwebjs-source Store.MsgActionChecks -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Store.js#L88
    // @wwebjs-source Store.Cmd -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Store.js#L50
    public async Task<bool> DeleteAsync(string messageId, bool everyone = false, bool clearMedia = true)
    {
        try
        {
            return await page.EvaluateAsync<bool>(
                """
                async (args) => {
                    const { messageId, everyone, clearMedia } = args;
                    let msg = window.Store.Msg.get(messageId);
                    if (!msg) {
                        const messagesObject = await window.Store.Msg.getMessagesById([messageId]);
                        msg = messagesObject?.messages?.[0];
                    }
                    if (!msg) return false;

                    const chat = window.Store.Chat.get(msg.id.remote) || (await window.Store.Chat.find(msg.id.remote));
                    if (!chat) return false;

                    const canRevoke = window.Store.MsgActionChecks.canSenderRevokeMsg(msg) ||
                                     window.Store.MsgActionChecks.canAdminRevokeMsg(msg);

                    if (everyone && canRevoke) {
                        await window.Store.Cmd.sendRevokeMsgs(chat, { list: [msg], type: 'message' }, { clearMedia: clearMedia });
                    } else {
                        await window.Store.Cmd.sendDeleteMsgs(chat, { list: [msg], type: 'message' }, clearMedia);
                    }

                    return true;
                }
                """, new { messageId, everyone, clearMedia });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting message: {MessageId}", messageId);
            return false;
        }
    }

    // @wwebjs-source Message.reply -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/Message.js#L380-L435
    // @wwebjs-source WWebJS.sendMessage -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L26-L369
    // @wwebjs-source WWebJS.getChat -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L568-L590
    public async Task<string?> ReplyAsync(string messageId, string content, string? chatId = null)
    {
        try
        {
            return await page.EvaluateAsync<string?>(
                """
                async (args) => {
                    const { messageId, content, chatId } = args;
                    let msg = window.Store.Msg.get(messageId) || (await window.Store.Msg.getMessagesById([messageId]))?.messages?.[0];
                    if (!msg) return null;
                    const targetChatId = chatId || (msg.id.fromMe ? msg.to?._serialized || msg.to : msg.from?._serialized || msg.from);
                    const chat = await window.WWebJS.getChat(targetChatId, { getAsModel: false });
                    if (!chat) return null;
                    const options = { quotedMessageId: msg.id._serialized };
                    const quotedMsg = window.Store.Msg.get(options.quotedMessageId);
                    let quotedMsgOptions = {};
                    if (quotedMsg) {
                        const canReply = window.Store.ReplyUtils ? window.Store.ReplyUtils.canReplyMsg(quotedMsg.unsafe()) : quotedMsg.canReply();
                        if (canReply) quotedMsgOptions = quotedMsg.msgContextInfo(chat);
                    }
                    const sentMsg = await window.WWebJS.sendMessage(chat, content, { ...quotedMsgOptions });
                    return sentMsg?.id?._serialized || null;
                }
                """, new { messageId, content, chatId });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error replying to message: {MessageId}", messageId);
            return null;
        }
    }

    // @wwebjs-source Message.react -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/Message.js#L437-L447
    // @wwebjs-source Store.sendReactionToMsg -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Store.js#L105
    public async Task<bool> ReactAsync(string messageId, string reaction)
    {
        try
        {
            return await page.EvaluateAsync<bool>(
                """
                async (args) => {
                    const [messageId, reaction] = args;
                    if (!messageId) return false;
                    const msg = window.Store.Msg.get(messageId) || (await window.Store.Msg.getMessagesById([messageId]))?.messages?.[0];
                    if (!msg) return false;
                    await window.Store.sendReactionToMsg(msg, reaction);
                    return true;
                }
                """, new object[] { messageId, reaction });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reacting to message: {MessageId}", messageId);
            return false;
        }
    }

    // @wwebjs-source Message.forward -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/Message.js#L449-L459
    // @wwebjs-source WWebJS.forwardMessage -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L371-L399
    public async Task<bool> ForwardAsync(string messageId, string chatId)
    {
        chatId = WhatsAppId.EnsureChatSuffix(chatId);

        try
        {
            await page.EvaluateAsync(
                """
                async (args) => {
                    const [msgId, chatId] = args;
                    await window.WWebJS.forwardMessage(chatId, msgId);
                }
                """, new object[] { messageId, chatId });
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error forwarding message: {MessageId} to {ChatId}", messageId, chatId);
            return false;
        }
    }

    // @wwebjs-source Message.star -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/Message.js#L461-L478
    // @wwebjs-source Store.MsgActionChecks.canStarMsg -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Store.js#L88
    // @wwebjs-source Store.Cmd.sendStarMsgs -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Store.js#L50
    public async Task<bool> StarAsync(string messageId)
    {
        try
        {
            return await page.EvaluateAsync<bool>(
                """
                async (msgId) => {
                    const msg = window.Store.Msg.get(msgId) || (await window.Store.Msg.getMessagesById([msgId]))?.messages?.[0];
                    if (!msg) return false;
                    if (window.Store.MsgActionChecks.canStarMsg(msg)) {
                        let chat = await window.Store.Chat.find(msg.id.remote);
                        await window.Store.Cmd.sendStarMsgs(chat, [msg], false);
                        return true;
                    }
                    return false;
                }
                """, messageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starring message: {MessageId}", messageId);
            return false;
        }
    }

    // @wwebjs-source Message.unstar -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/Message.js#L480-L497
    // @wwebjs-source Store.MsgActionChecks.canStarMsg -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Store.js#L88
    // @wwebjs-source Store.Cmd.sendUnstarMsgs -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Store.js#L50
    public async Task<bool> UnstarAsync(string messageId)
    {
        try
        {
            return await page.EvaluateAsync<bool>(
                """
                async (msgId) => {
                    const msg = window.Store.Msg.get(msgId) || (await window.Store.Msg.getMessagesById([msgId]))?.messages?.[0];
                    if (!msg) return false;
                    if (window.Store.MsgActionChecks.canStarMsg(msg)) {
                        let chat = await window.Store.Chat.find(msg.id.remote);
                        await window.Store.Cmd.sendUnstarMsgs(chat, [msg], false);
                        return true;
                    }
                    return false;
                }
                """, messageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error unstarring message: {MessageId}", messageId);
            return false;
        }
    }

    // @wwebjs-source Message.pin -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/Message.js#L499-L507
    // @wwebjs-source WWebJS.pinUnpinMsgAction -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L401-L430
    public async Task<bool> PinAsync(string messageId, int durationSeconds)
    {
        try
        {
            return await page.EvaluateAsync<bool>(
                """
                async (args) => {
                    const [msgId, duration] = args;
                    return await window.WWebJS.pinUnpinMsgAction(msgId, 1, duration);
                }
                """, new object[] { messageId, durationSeconds });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error pinning message: {MessageId}", messageId);
            return false;
        }
    }

    // @wwebjs-source Message.unpin -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/Message.js#L499-L507
    // @wwebjs-source WWebJS.pinUnpinMsgAction -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L401-L430
    public async Task<bool> UnpinAsync(string messageId)
    {
        try
        {
            return await page.EvaluateAsync<bool>(
                """
                async (msgId) => {
                    return await window.WWebJS.pinUnpinMsgAction(msgId, 2, 0);
                }
                """, messageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error unpinning message: {MessageId}", messageId);
            return false;
        }
    }

    // @wwebjs-source Message.getInfo -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/Message.js#L294-L317
    // @wwebjs-source Store.getMsgInfo -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Store.js#L87
    public async Task<MessageInfoModel?> GetInfoAsync(string messageId)
    {
        try
        {
            var jsonStr = await page.EvaluateAsync<string?>(
                """
                async (msgId) => {
                    const msg = window.Store.Msg.get(msgId) || (await window.Store.Msg.getMessagesById([msgId]))?.messages?.[0];
                    if (!msg || !msg.id.fromMe) return null;
                    const info = await new Promise((resolve) => {
                        setTimeout(async () => {
                            resolve(await window.Store.getMsgInfo(msg.id));
                        }, (Date.now() - msg.t * 1000 < 1250) && Math.floor(Math.random() * (1200 - 1100 + 1)) + 1100 || 0);
                    });
                    return JSON.stringify(info);
                }
                """, messageId);
            return string.IsNullOrEmpty(jsonStr) ? null : JsonSerializer.Deserialize<MessageInfoModel>(jsonStr, jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting message info: {MessageId}", messageId);
            return null;
        }
    }

    // @wwebjs-source Message.edit -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/Message.js#L319-L340
    // @wwebjs-source WWebJS.editMessage -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L432-L460
    // @wwebjs-source Store.MsgActionChecks.canEditText -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Store.js#L88
    // @wwebjs-source Store.MsgActionChecks.canEditCaption -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Store.js#L88
    public async Task<bool> EditAsync(string messageId, string content)
    {
        try
        {
            return await page.EvaluateAsync<bool>(
                """
                async (args) => {
                    const [msgId, content] = args;
                    const msg = window.Store.Msg.get(msgId) || (await window.Store.Msg.getMessagesById([msgId]))?.messages?.[0];
                    if (!msg) return false;
                    let canEdit = window.Store.MsgActionChecks.canEditText(msg) || window.Store.MsgActionChecks.canEditCaption(msg);
                    if (!canEdit) return false;
                    await window.WWebJS.editMessage(msg, content, {});
                    return true;
                }
                """, new object[] { messageId, content });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error editing message: {MessageId}", messageId);
            return false;
        }
    }

    // @wwebjs-source Message.getReactions -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/Message.js#L342-L358
    // @wwebjs-source Store.Reactions -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Store.js#L100
    public async Task<string?> GetReactionsAsync(string messageId)
    {
        try
        {
            return await page.EvaluateAsync<string?>(
                """
                async (msgId) => {
                    const msgReactions = await window.Store.Reactions.find(msgId);
                    if (!msgReactions || !msgReactions.reactions.length) return null;
                    return JSON.stringify(msgReactions.reactions.serialize());
                }
                """, messageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting message reactions: {MessageId}", messageId);
            return null;
        }
    }
}

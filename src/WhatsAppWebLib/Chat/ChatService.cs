using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using WhatsAppWebLib.Message;

namespace WhatsAppWebLib.Chat;

public class ChatService(IPage page, JsonSerializerOptions jsonOptions, ILogger logger)
{
    // @wwebjs-source WWebJS.getChats -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L621-L625
    // @wwebjs-source Client.getChats -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1161-L1167
    // @wwebjs-source WWebJS.getChatModel -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L633-L677
    public async Task<List<ChatModel>> GetChatsAsync()
    {
        try
        {
            var jsonStr = await page.EvaluateAsync<string>(
                """
                async () => {
                    const chats = await window.WWebJS.getChats();
                    return JSON.stringify(chats.map(c => ({
                        id: c.id?._serialized ?? c.id,
                        name: c.formattedTitle,
                        isGroup: c.isGroup ?? false,
                        isReadOnly: !!c.isReadOnly,
                        unreadCount: c.unreadCount ?? 0,
                        timestamp: c.t,
                        archived: c.archive ?? false,
                        pinned: !!c.pin,
                        isMuted: c.isMuted ?? false,
                        muteExpiration: c.muteExpiration
                    })));
                }
                """);
            return JsonSerializer.Deserialize<List<ChatModel>>(jsonStr, jsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting chats");
            return [];
        }
    }

    // @wwebjs-source WWebJS.getChat -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L568-L590
    // @wwebjs-source Client.getChatById -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1186-L1197
    public async Task<ChatModel?> GetChatByIdAsync(string chatId)
    {
        chatId = WhatsAppId.EnsureChatSuffix(chatId);

        try
        {
            var jsonStr = await page.EvaluateAsync<string?>(
                """
                async (chatId) => {
                    const chat = await window.WWebJS.getChat(chatId);
                    if (!chat) return null;
                    return JSON.stringify({
                        id: chat.id?._serialized ?? chat.id,
                        name: chat.formattedTitle,
                        isGroup: chat.isGroup ?? false,
                        isReadOnly: !!chat.isReadOnly,
                        unreadCount: chat.unreadCount ?? 0,
                        timestamp: chat.t,
                        archived: chat.archive ?? false,
                        pinned: !!chat.pin,
                        isMuted: chat.isMuted ?? false,
                        muteExpiration: chat.muteExpiration
                    });
                }
                """, chatId);
            return string.IsNullOrEmpty(jsonStr) ? null : JsonSerializer.Deserialize<ChatModel>(jsonStr, jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting chat: {ChatId}", chatId);
            return null;
        }
    }

    // @wwebjs-source WWebJS.getChat -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L568-L590
    // @wwebjs-source WWebJS.getChatModel -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L633-L677
    // @wwebjs-source GroupChat.participants -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/GroupChat.js#L52-L53
    public async Task<ChatInfoModel?> GetChatInfoAsync(string chatId)
    {
        chatId = WhatsAppId.EnsureChatSuffix(chatId);

        try
        {
            var jsonStr = await page.EvaluateAsync<string?>(
                """
                async (chatId) => {
                    const chat = await window.WWebJS.getChat(chatId);
                    if (!chat) return null;

                    const result = {
                        id: chat.id?._serialized ?? chat.id,
                        name: chat.formattedTitle,
                        isGroup: chat.isGroup ?? false,
                        isReadOnly: !!chat.isReadOnly,
                        unreadCount: chat.unreadCount ?? 0,
                        timestamp: chat.t,
                        archived: chat.archive ?? false,
                        pinned: !!chat.pin,
                        isMuted: chat.isMuted ?? false,
                        muteExpiration: chat.muteExpiration,
                        lastMessage: null,
                        groupMetadata: null
                    };

                    if (chat.lastMessage) {
                        result.lastMessage = {
                            id: chat.lastMessage.id?._serialized ?? chat.lastMessage.id,
                            body: chat.lastMessage.body,
                            from: chat.lastMessage.from?._serialized ?? chat.lastMessage.from,
                            to: chat.lastMessage.to?._serialized ?? chat.lastMessage.to,
                            fromMe: chat.lastMessage.id?.fromMe ?? false,
                            timestamp: chat.lastMessage.t,
                            type: chat.lastMessage.type,
                            hasMedia: chat.lastMessage.hasMedia ?? false
                        };
                    }

                    if (chat.isGroup && chat.groupMetadata) {
                        result.groupMetadata = {
                            owner: chat.groupMetadata.owner?._serialized ?? chat.groupMetadata.owner,
                            creation: chat.groupMetadata.creation,
                            description: chat.groupMetadata.desc,
                            participants: (chat.groupMetadata.participants ?? []).map(p => ({
                                id: p.id?._serialized ?? p.id,
                                isAdmin: p.isAdmin ?? false,
                                isSuperAdmin: p.isSuperAdmin ?? false
                            }))
                        };
                    }

                    return JSON.stringify(result);
                }
                """, chatId);
            return string.IsNullOrEmpty(jsonStr) ? null : JsonSerializer.Deserialize<ChatInfoModel>(jsonStr, jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting chat info: {ChatId}", chatId);
            return null;
        }
    }

    // @wwebjs-source WWebJS.sendSeen -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L318-L327
    // @wwebjs-source Client.sendSeen -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1090-L1098
    public async Task<bool> SendSeenAsync(string chatId)
    {
        chatId = WhatsAppId.EnsureChatSuffix(chatId);

        try
        {
            var result = await page.EvaluateAsync<bool>(
                """
                async (chatId) => {
                    return window.WWebJS.sendSeen(chatId);
                }
                """, chatId);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending seen: {ChatId}", chatId);
            return false;
        }
    }

    // @wwebjs-source Client.archiveChat -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1448-L1455
    // @wwebjs-source Store.Cmd.archiveChat -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Store.js#L50
    public async Task<bool> ArchiveAsync(string chatId)
    {
        chatId = WhatsAppId.EnsureChatSuffix(chatId);

        try
        {
            return await page.EvaluateAsync<bool>(
                """
                async (chatId) => {
                    let chat = await window.WWebJS.getChat(chatId, { getAsModel: false });
                    await window.Store.Cmd.archiveChat(chat, true);
                    return true;
                }
                """, chatId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error archiving chat: {ChatId}", chatId);
            return false;
        }
    }

    // @wwebjs-source Client.unarchiveChat -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1460-L1467
    // @wwebjs-source Store.Cmd.archiveChat -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Store.js#L50
    public async Task<bool> UnarchiveAsync(string chatId)
    {
        chatId = WhatsAppId.EnsureChatSuffix(chatId);

        try
        {
            await page.EvaluateAsync(
                """
                async (chatId) => {
                    let chat = await window.WWebJS.getChat(chatId, { getAsModel: false });
                    await window.Store.Cmd.archiveChat(chat, false);
                }
                """, chatId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error unarchiving chat: {ChatId}", chatId);
            return false;
        }
    }

    // @wwebjs-source Client.pinChat -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1472-L1493
    // @wwebjs-source Store.Cmd.pinChat -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Store.js#L50
    public async Task<bool> PinAsync(string chatId)
    {
        chatId = WhatsAppId.EnsureChatSuffix(chatId);

        try
        {
            return await page.EvaluateAsync<bool>(
                """
                async (chatId) => {
                    let chat = await window.WWebJS.getChat(chatId, { getAsModel: false });
                    if (chat.pin) return true;
                    const MAX_PIN_COUNT = 3;
                    const chatModels = window.Store.Chat.getModelsArray();
                    if (chatModels.length > MAX_PIN_COUNT) {
                        let maxPinned = chatModels[MAX_PIN_COUNT - 1].pin;
                        if (maxPinned) return false;
                    }
                    await window.Store.Cmd.pinChat(chat, true);
                    return true;
                }
                """, chatId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error pinning chat: {ChatId}", chatId);
            return false;
        }
    }

    // @wwebjs-source Client.unpinChat -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1495-L1507
    // @wwebjs-source Store.Cmd.pinChat -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Store.js#L50
    public async Task<bool> UnpinAsync(string chatId)
    {
        chatId = WhatsAppId.EnsureChatSuffix(chatId);

        try
        {
            await page.EvaluateAsync(
                """
                async (chatId) => {
                    let chat = await window.WWebJS.getChat(chatId, { getAsModel: false });
                    if (!chat.pin) return;
                    await window.Store.Cmd.pinChat(chat, false);
                }
                """, chatId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error unpinning chat: {ChatId}", chatId);
            return false;
        }
    }

    // @wwebjs-source Client.muteChat -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1277-L1294
    // @wwebjs-source WWebJS.getChat -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L568-L590
    public async Task<MuteResult?> MuteAsync(string chatId, long? unmuteTimestamp = null)
    {
        chatId = WhatsAppId.EnsureChatSuffix(chatId);

        try
        {
            var jsonStr = await page.EvaluateAsync<string>(
                """
                async (args) => {
                    const [chatId, unmuteDate] = args;
                    const chat = await window.WWebJS.getChat(chatId, { getAsModel: false });
                    const ms = unmuteDate ? unmuteDate : -1;
                    await chat.mute.mute({expiration: ms, sendDevice: true});
                    return JSON.stringify({ isMuted: chat.mute.expiration !== 0, muteExpiration: chat.mute.expiration });
                }
                """, new object?[] { chatId, unmuteTimestamp });
            return JsonSerializer.Deserialize<MuteResult>(jsonStr, jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error muting chat: {ChatId}", chatId);
            return null;
        }
    }

    // @wwebjs-source Client.unmuteChat -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1302-L1316
    // @wwebjs-source WWebJS.getChat -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L568-L590
    public async Task<MuteResult?> UnmuteAsync(string chatId)
    {
        chatId = WhatsAppId.EnsureChatSuffix(chatId);

        try
        {
            var jsonStr = await page.EvaluateAsync<string>(
                """
                async (chatId) => {
                    const chat = await window.WWebJS.getChat(chatId, { getAsModel: false });
                    await chat.mute.unmute({sendDevice: true});
                    return JSON.stringify({ isMuted: chat.mute.expiration !== 0, muteExpiration: chat.mute.expiration });
                }
                """, chatId);
            return JsonSerializer.Deserialize<MuteResult>(jsonStr, jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error unmuting chat: {ChatId}", chatId);
            return null;
        }
    }

    // @wwebjs-source Client.markChatUnread -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1324-L1335
    // @wwebjs-source WWebJS.getChat -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L568-L590
    public async Task<bool> MarkUnreadAsync(string chatId)
    {
        chatId = WhatsAppId.EnsureChatSuffix(chatId);

        try
        {
            var result = await page.EvaluateAsync<bool>(
                """
                async (chatId) => {
                    const chat = await window.WWebJS.getChat(chatId, { getAsModel: false });
                    await window.Store.Cmd.markChatUnread(chat, true);
                    return true;
                }
                """, chatId);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error marking chat as unread: {ChatId}", chatId);
            return false;
        }
    }

    // @wwebjs-source Chat.clearMessages -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/Chat.js#L120-L125
    // @wwebjs-source WWebJS.sendClearChat -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L365-L377
    public async Task<bool> ClearMessagesAsync(string chatId)
    {
        chatId = WhatsAppId.EnsureChatSuffix(chatId);

        try
        {
            var result = await page.EvaluateAsync<bool>(
                """
                async (chatId) => {
                    return window.WWebJS.sendClearChat(chatId);
                }
                """, chatId);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error clearing chat messages: {ChatId}", chatId);
            return false;
        }
    }

    // @wwebjs-source Chat.delete -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/Chat.js#L132-L137
    // @wwebjs-source WWebJS.sendDeleteChat -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L379-L391
    public async Task<bool> DeleteAsync(string chatId)
    {
        chatId = WhatsAppId.EnsureChatSuffix(chatId);

        try
        {
            var result = await page.EvaluateAsync<bool>(
                """
                async (chatId) => {
                    return window.WWebJS.sendDeleteChat(chatId);
                }
                """, chatId);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting chat: {ChatId}", chatId);
            return false;
        }
    }

    // @wwebjs-source Client.sendPresenceAvailable -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1343-L1355
    // @wwebjs-source WWebJS.sendChatstate -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L393-L410
    public async Task<bool> SendStateTypingAsync(string chatId)
    {
        chatId = WhatsAppId.EnsureChatSuffix(chatId);

        try
        {
            var result = await page.EvaluateAsync<bool>(
                """
                async (chatId) => {
                    return window.WWebJS.sendChatstate('typing', chatId);
                }
                """, chatId);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending typing state: {ChatId}", chatId);
            return false;
        }
    }

    // @wwebjs-source Client.sendPresenceAvailable -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1343-L1355
    // @wwebjs-source WWebJS.sendChatstate -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L393-L410
    public async Task<bool> SendStateRecordingAsync(string chatId)
    {
        chatId = WhatsAppId.EnsureChatSuffix(chatId);

        try
        {
            var result = await page.EvaluateAsync<bool>(
                """
                async (chatId) => {
                    return window.WWebJS.sendChatstate('recording', chatId);
                }
                """, chatId);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending recording state: {ChatId}", chatId);
            return false;
        }
    }

    // @wwebjs-source Client.sendPresenceAvailable -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1343-L1355
    // @wwebjs-source WWebJS.sendChatstate -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L393-L410
    public async Task<bool> ClearStateAsync(string chatId)
    {
        chatId = WhatsAppId.EnsureChatSuffix(chatId);

        try
        {
            var result = await page.EvaluateAsync<bool>(
                """
                async (chatId) => {
                    return window.WWebJS.sendChatstate('stop', chatId);
                }
                """, chatId);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error clearing chat state: {ChatId}", chatId);
            return false;
        }
    }

    // @wwebjs-source Client.searchMessages -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1363-L1381
    // @wwebjs-source WWebJS.getMessageModel -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L440-L500
    public async Task<List<MessageModel>> SearchMessagesAsync(string query, string? chatId = null, int? pageNumber = null, int? limit = null)
    {
        if (chatId != null)
            chatId = WhatsAppId.EnsureChatSuffix(chatId);

        try
        {
            var jsonStr = await page.EvaluateAsync<string>(
                """
                async (args) => {
                    const { query, chatId, page, limit } = args;
                    const result = await window.Store.Msg.search(query, page || 1, limit || 10, chatId || '');
                    const messages = result?.messages || result || [];
                    return JSON.stringify(Array.isArray(messages) ? messages.map(m => window.WWebJS.getMessageModel(m)) : []);
                }
                """, new { query, chatId = chatId ?? "", page = pageNumber ?? 1, limit = limit ?? 10 });
            return JsonSerializer.Deserialize<List<MessageModel>>(jsonStr, jsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching messages: {Query}, ChatId: {ChatId}", query, chatId);
            return [];
        }
    }
}

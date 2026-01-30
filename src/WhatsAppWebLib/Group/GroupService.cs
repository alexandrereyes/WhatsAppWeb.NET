using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace WhatsAppWebLib.Group;

public class GroupService(IPage page, JsonSerializerOptions jsonOptions, ILogger logger)
{
    // @wwebjs-source WWebJS.getChat -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L568-L590
    // @wwebjs-source GroupChat.participants -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/GroupChat.js#L52-L53
    // @wwebjs-source Utils.js participants serialization -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L645-L654
    public async Task<List<GroupParticipant>?> GetParticipantsAsync(string groupId)
    {
        groupId = WhatsAppId.EnsureGroupSuffix(groupId);

        try
        {
            var jsonStr = await page.EvaluateAsync<string?>(
                """
                async (groupId) => {
                    const chat = await window.WWebJS.getChat(groupId);
                    if (!chat || !chat.isGroup) return null;
                    if (!chat.groupMetadata?.participants) return null;

                    return JSON.stringify(chat.groupMetadata.participants.map(p => ({
                        id: p.id?._serialized ?? p.id,
                        isAdmin: p.isAdmin ?? false,
                        isSuperAdmin: p.isSuperAdmin ?? false
                    })));
                }
                """, groupId);
            return string.IsNullOrEmpty(jsonStr) ? null : JsonSerializer.Deserialize<List<GroupParticipant>>(jsonStr, jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting participants: {GroupId}", groupId);
            return null;
        }
    }

    // @wwebjs-source Client.createGroup -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1330-L1388
    public async Task<string?> CreateGroupAsync(string title, string[] participantIds)
    {
        try
        {
            var result = await page.EvaluateAsync<string?>(
                """
                async (args) => {
                    const { title, participantIds } = args;
                    const wids = participantIds.map(p => window.Store.WidFactory.createWid(p));
                    const createGroupResult = await window.Store.GroupUtils.createGroup(title, wids, 0);
                    return createGroupResult?.gid?._serialized || null;
                }
                """, new { title, participantIds });
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating group: {Title}", title);
            return null;
        }
    }

    // @wwebjs-source GroupChat.addParticipants -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/GroupChat.js#L80-L165
    public async Task<bool> AddParticipantsAsync(string groupId, string[] participantIds)
    {
        groupId = WhatsAppId.EnsureGroupSuffix(groupId);

        try
        {
            await page.EvaluateAsync<bool>(
                """
                async (args) => {
                    const { groupId, participantIds } = args;
                    const groupWid = window.Store.WidFactory.createWid(groupId);
                    const group = window.Store.Chat.get(groupWid) || (await window.Store.Chat.find(groupWid));
                    for (const pId of participantIds) {
                        const pWid = window.Store.WidFactory.createWid(pId);
                        await window.WWebJS.getAddParticipantsRpcResult(groupWid, pWid);
                    }
                    return true;
                }
                """, new { groupId, participantIds });
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding participants: {GroupId}", groupId);
            return false;
        }
    }

    // @wwebjs-source GroupChat.removeParticipants -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/GroupChat.js#L173-L185
    public async Task<bool> RemoveParticipantsAsync(string groupId, string[] participantIds)
    {
        groupId = WhatsAppId.EnsureGroupSuffix(groupId);

        try
        {
            await page.EvaluateAsync<bool>(
                """
                async (args) => {
                    const { groupId, participantIds } = args;
                    const chat = await window.WWebJS.getChat(groupId, { getAsModel: false });
                    const participants = (await Promise.all(participantIds.map(async p => {
                        const { lid, phone } = await window.WWebJS.enforceLidAndPnRetrieval(p);
                        return chat.groupMetadata.participants.get(lid?._serialized) || chat.groupMetadata.participants.get(phone?._serialized);
                    }))).filter(Boolean);
                    await window.Store.GroupParticipants.removeParticipants(chat, participants);
                    return true;
                }
                """, new { groupId, participantIds });
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing participants: {GroupId}", groupId);
            return false;
        }
    }

    // @wwebjs-source GroupChat.promoteParticipants -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/GroupChat.js#L193-L206
    public async Task<bool> PromoteParticipantsAsync(string groupId, string[] participantIds)
    {
        groupId = WhatsAppId.EnsureGroupSuffix(groupId);

        try
        {
            await page.EvaluateAsync<bool>(
                """
                async (args) => {
                    const { groupId, participantIds } = args;
                    const chat = await window.WWebJS.getChat(groupId, { getAsModel: false });
                    const participants = (await Promise.all(participantIds.map(async p => {
                        const { lid, phone } = await window.WWebJS.enforceLidAndPnRetrieval(p);
                        return chat.groupMetadata.participants.get(lid?._serialized) || chat.groupMetadata.participants.get(phone?._serialized);
                    }))).filter(Boolean);
                    await window.Store.GroupParticipants.promoteParticipants(chat, participants);
                    return true;
                }
                """, new { groupId, participantIds });
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error promoting participants: {GroupId}", groupId);
            return false;
        }
    }

    // @wwebjs-source GroupChat.demoteParticipants -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/GroupChat.js#L214-L227
    public async Task<bool> DemoteParticipantsAsync(string groupId, string[] participantIds)
    {
        groupId = WhatsAppId.EnsureGroupSuffix(groupId);

        try
        {
            await page.EvaluateAsync<bool>(
                """
                async (args) => {
                    const { groupId, participantIds } = args;
                    const chat = await window.WWebJS.getChat(groupId, { getAsModel: false });
                    const participants = (await Promise.all(participantIds.map(async p => {
                        const { lid, phone } = await window.WWebJS.enforceLidAndPnRetrieval(p);
                        return chat.groupMetadata.participants.get(lid?._serialized) || chat.groupMetadata.participants.get(phone?._serialized);
                    }))).filter(Boolean);
                    await window.Store.GroupParticipants.demoteParticipants(chat, participants);
                    return true;
                }
                """, new { groupId, participantIds });
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error demoting participants: {GroupId}", groupId);
            return false;
        }
    }

    // @wwebjs-source GroupChat.setSubject -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/GroupChat.js#L234-L247
    public async Task<bool> SetSubjectAsync(string groupId, string subject)
    {
        groupId = WhatsAppId.EnsureGroupSuffix(groupId);

        try
        {
            return await page.EvaluateAsync<bool>(
                """
                async (args) => {
                    const [groupId, subject] = args;
                    const chatWid = window.Store.WidFactory.createWid(groupId);
                    try {
                        await window.Store.GroupUtils.setGroupSubject(chatWid, subject);
                        return true;
                    } catch (err) {
                        if (err.name === 'ServerStatusCodeError') return false;
                        throw err;
                    }
                }
                """, new object[] { groupId, subject });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting group subject: {GroupId}", groupId);
            return false;
        }
    }

    // @wwebjs-source GroupChat.setDescription -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/GroupChat.js#L256-L269
    public async Task<bool> SetDescriptionAsync(string groupId, string description)
    {
        groupId = WhatsAppId.EnsureGroupSuffix(groupId);

        try
        {
            return await page.EvaluateAsync<bool>(
                """
                async (args) => {
                    const [groupId, description] = args;
                    const chatWid = window.Store.WidFactory.createWid(groupId);
                    let descId = window.Store.GroupMetadata.get(chatWid).descId;
                    let newId = await window.Store.MsgKey.newId();
                    try {
                        await window.Store.GroupUtils.setGroupDescription(chatWid, description, newId, descId);
                        return true;
                    } catch (err) {
                        if (err.name === 'ServerStatusCodeError') return false;
                        throw err;
                    }
                }
                """, new object[] { groupId, description });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting group description: {GroupId}", groupId);
            return false;
        }
    }

    // @wwebjs-source GroupChat.setMessagesAdminsOnly -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/GroupChat.js#L295-L308
    public async Task<bool> SetMessagesAdminsOnlyAsync(string groupId, bool adminsOnly = true)
    {
        groupId = WhatsAppId.EnsureGroupSuffix(groupId);

        try
        {
            return await page.EvaluateAsync<bool>(
                """
                async (args) => {
                    const [groupId, adminsOnly] = args;
                    const chat = await window.WWebJS.getChat(groupId, { getAsModel: false });
                    try {
                        await window.Store.GroupUtils.setGroupProperty(chat, 'announcement', adminsOnly ? 1 : 0);
                        return true;
                    } catch (err) {
                        if (err.name === 'ServerStatusCodeError') return false;
                        throw err;
                    }
                }
                """, new object[] { groupId, adminsOnly });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting admin-only messages: {GroupId}", groupId);
            return false;
        }
    }

    // @wwebjs-source GroupChat.setInfoAdminsOnly -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/GroupChat.js#L317-L332
    public async Task<bool> SetInfoAdminsOnlyAsync(string groupId, bool adminsOnly = true)
    {
        groupId = WhatsAppId.EnsureGroupSuffix(groupId);

        try
        {
            return await page.EvaluateAsync<bool>(
                """
                async (args) => {
                    const [groupId, adminsOnly] = args;
                    const chat = await window.WWebJS.getChat(groupId, { getAsModel: false });
                    try {
                        await window.Store.GroupUtils.setGroupProperty(chat, 'restrict', adminsOnly ? 1 : 0);
                        return true;
                    } catch (err) {
                        if (err.name === 'ServerStatusCodeError') return false;
                        throw err;
                    }
                }
                """, new object[] { groupId, adminsOnly });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting admin-only info: {GroupId}", groupId);
            return false;
        }
    }

    // @wwebjs-source GroupChat.getInviteCode -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/GroupChat.js#L339-L354
    public async Task<string?> GetInviteCodeAsync(string groupId)
    {
        groupId = WhatsAppId.EnsureGroupSuffix(groupId);

        try
        {
            return await page.EvaluateAsync<string?>(
                """
                async (groupId) => {
                    const chatWid = window.Store.WidFactory.createWid(groupId);
                    try {
                        const codeRes = window.compareWwebVersions(window.Debug.VERSION, '>=', '2.3000.1020730154')
                            ? await window.Store.GroupInvite.fetchMexGroupInviteCode(groupId)
                            : await window.Store.GroupInvite.queryGroupInviteCode(chatWid, true);
                        return codeRes?.code || codeRes || null;
                    } catch (err) {
                        if (err.name === 'ServerStatusCodeError') return null;
                        throw err;
                    }
                }
                """, groupId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting invite code: {GroupId}", groupId);
            return null;
        }
    }

    // @wwebjs-source GroupChat.revokeInvite -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/GroupChat.js#L361-L367
    public async Task<string?> RevokeInviteAsync(string groupId)
    {
        groupId = WhatsAppId.EnsureGroupSuffix(groupId);

        try
        {
            return await page.EvaluateAsync<string?>(
                """
                async (groupId) => {
                    const chatWid = window.Store.WidFactory.createWid(groupId);
                    const codeRes = await window.Store.GroupInvite.resetGroupInviteCode(chatWid);
                    return codeRes?.code || null;
                }
                """, groupId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error revoking invite: {GroupId}", groupId);
            return null;
        }
    }

    // @wwebjs-source Client.getInviteInfo -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1273-L1285
    public async Task<string?> GetInviteInfoAsync(string inviteCode)
    {
        try
        {
            return await page.EvaluateAsync<string?>(
                """
                async (inviteCode) => {
                    const info = await window.Store.InviteInfo.queryGroupInvite(inviteCode);
                    return info ? JSON.stringify(info) : null;
                }
                """, inviteCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting invite info: {InviteCode}", inviteCode);
            return null;
        }
    }

    // @wwebjs-source Client.acceptInvite -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1293-L1303
    public async Task<string?> AcceptInviteAsync(string inviteCode)
    {
        try
        {
            return await page.EvaluateAsync<string?>(
                """
                async (inviteCode) => {
                    const res = await window.Store.InviteInfo.joinGroupViaInvite(inviteCode);
                    if (res.gid) return res.gid._serialized;
                    return null;
                }
                """, inviteCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error accepting invite: {InviteCode}", inviteCode);
            return null;
        }
    }

    // @wwebjs-source GroupChat.leave -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/GroupChat.js#L416-L420
    public async Task<bool> LeaveAsync(string groupId)
    {
        groupId = WhatsAppId.EnsureGroupSuffix(groupId);

        try
        {
            await page.EvaluateAsync<bool>(
                """
                async (groupId) => {
                    const chat = await window.WWebJS.getChat(groupId, { getAsModel: false });
                    await window.Store.GroupUtils.sendExitGroup(chat);
                    return true;
                }
                """, groupId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error leaving group: {GroupId}", groupId);
            return false;
        }
    }
}

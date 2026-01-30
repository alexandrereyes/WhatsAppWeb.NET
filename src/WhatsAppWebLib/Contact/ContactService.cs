using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace WhatsAppWebLib.Contact;

public class ContactService(IPage page, JsonSerializerOptions jsonOptions, ILogger logger)
{
    // @wwebjs-source Client.getNumberId -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1629-L1640
    // @wwebjs-source Store.WidFactory -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Store.js#L79
    // @wwebjs-source Store.QueryExist -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Store.js#L99
    public async Task<NumberIdResult?> GetNumberIdAsync(string number)
    {
        number = WhatsAppId.EnsureChatSuffix(number);

        try
        {
            return await page.EvaluateAsync<NumberIdResult?>(
                """
                async (number) => {
                    const wid = window.Store.WidFactory.createWid(number);
                    const result = await window.Store.QueryExist(wid);
                    if (!result || result.wid === undefined) return null;
                    return {
                        wid: result.wid._serialized,
                        isBusiness: result.biz || false,
                        verifiedName: result.bizInfo?.verifiedName?.name || null
                    };
                }
                """, number);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying number: {Number}", number);
            return null;
        }
    }

    // @wwebjs-source Client.getContacts -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1207-L1213
    // @wwebjs-source WWebJS.getContacts -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L729-L732
    public async Task<List<ContactModel>> GetContactsAsync()
    {
        try
        {
            var json = await page.EvaluateAsync<string>(
                """
                async () => {
                    const contacts = window.WWebJS.getContacts();
                    return JSON.stringify(contacts.map(c => ({
                        id: c.id?._serialized ?? c.id,
                        number: c.userid,
                        name: c.name,
                        pushname: c.pushname,
                        shortName: c.shortName,
                        isMe: c.isMe ?? false,
                        isUser: c.isUser ?? false,
                        isGroup: c.isGroup ?? false,
                        isWAContact: c.isWAContact ?? false,
                        isMyContact: c.isMyContact ?? false,
                        isBusiness: c.isBusiness ?? false,
                        isBlocked: c.isBlocked ?? false
                    })));
                }
                """);

            return JsonSerializer.Deserialize<List<ContactModel>>(json, jsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting contacts");
            return [];
        }
    }

    // @wwebjs-source Client.getContactById -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1221-L1228
    // @wwebjs-source WWebJS.getContact -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L720-L727
    public async Task<ContactModel?> GetContactByIdAsync(string contactId)
    {
        contactId = WhatsAppId.EnsureChatSuffix(contactId);

        try
        {
            var json = await page.EvaluateAsync<string?>(
                """
                async (contactId) => {
                    const contact = await window.WWebJS.getContact(contactId);
                    if (!contact) return null;
                    return JSON.stringify({
                        id: contact.id?._serialized ?? contact.id,
                        number: contact.userid,
                        name: contact.name,
                        pushname: contact.pushname,
                        shortName: contact.shortName,
                        isMe: contact.isMe ?? false,
                        isUser: contact.isUser ?? false,
                        isGroup: contact.isGroup ?? false,
                        isWAContact: contact.isWAContact ?? false,
                        isMyContact: contact.isMyContact ?? false,
                        isBusiness: contact.isBusiness ?? false,
                        isBlocked: contact.isBlocked ?? false
                    });
                }
                """, contactId);

            if (json is null) return null;
            return JsonSerializer.Deserialize<ContactModel>(json, jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting contact: {ContactId}", contactId);
            return null;
        }
    }

    // @wwebjs-source Client.getBlockedContacts -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1541-L1549
    public async Task<List<ContactModel>> GetBlockedContactsAsync()
    {
        try
        {
            var json = await page.EvaluateAsync<string>(
                """
                async () => {
                    const chatIds = window.Store.Blocklist.getModelsArray().map(a => a.id._serialized);
                    const contacts = await Promise.all(chatIds.map(id => window.WWebJS.getContact(id)));
                    return JSON.stringify(contacts.filter(Boolean).map(c => ({
                        id: c.id?._serialized ?? c.id,
                        number: c.userid,
                        name: c.name,
                        pushname: c.pushname,
                        shortName: c.shortName,
                        isMe: c.isMe ?? false,
                        isUser: c.isUser ?? false,
                        isGroup: c.isGroup ?? false,
                        isWAContact: c.isWAContact ?? false,
                        isMyContact: c.isMyContact ?? false,
                        isBusiness: c.isBusiness ?? false,
                        isBlocked: c.isBlocked ?? false
                    })));
                }
                """);

            return JsonSerializer.Deserialize<List<ContactModel>>(json, jsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting blocked contacts");
            return [];
        }
    }

    // @wwebjs-source Client.isRegisteredUser -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1620-L1627
    public async Task<bool> IsRegisteredUserAsync(string contactId)
    {
        contactId = WhatsAppId.EnsureChatSuffix(contactId);

        try
        {
            return await page.EvaluateAsync<bool>(
                """
                async (contactId) => {
                    const wid = window.Store.WidFactory.createWid(contactId);
                    const result = await window.Store.QueryExist(wid);
                    return result?.wid ? true : false;
                }
                """, contactId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking registered user: {ContactId}", contactId);
            return false;
        }
    }

    // @wwebjs-source Contact.block -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/Contact.js#L123-L133
    public async Task<bool> BlockAsync(string contactId)
    {
        contactId = WhatsAppId.EnsureChatSuffix(contactId);

        try
        {
            return await page.EvaluateAsync<bool>(
                """
                async (contactId) => {
                    const contact = window.Store.Contact.get(contactId);
                    if (!contact) return false;
                    await window.Store.BlockContact.blockContact({contact});
                    return true;
                }
                """, contactId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error blocking contact: {ContactId}", contactId);
            return false;
        }
    }

    // @wwebjs-source Contact.unblock -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/Contact.js#L140-L150
    public async Task<bool> UnblockAsync(string contactId)
    {
        contactId = WhatsAppId.EnsureChatSuffix(contactId);

        try
        {
            return await page.EvaluateAsync<bool>(
                """
                async (contactId) => {
                    const contact = window.Store.Contact.get(contactId);
                    if (!contact) return false;
                    await window.Store.BlockContact.unblockContact(contact);
                    return true;
                }
                """, contactId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error unblocking contact: {ContactId}", contactId);
            return false;
        }
    }

    // @wwebjs-source Contact.getAbout -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/Contact.js#L157-L166
    public async Task<string?> GetAboutAsync(string contactId)
    {
        contactId = WhatsAppId.EnsureChatSuffix(contactId);

        try
        {
            return await page.EvaluateAsync<string?>(
                """
                async (contactId) => {
                    const wid = window.Store.WidFactory.createWid(contactId);
                    const about = await window.Store.StatusUtils.getStatus({'token':'', 'wid': wid});
                    return typeof about.status === 'string' ? about.status : null;
                }
                """, contactId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting contact about: {ContactId}", contactId);
            return null;
        }
    }

    // @wwebjs-source Client.getCommonGroups -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1580-L1603
    public async Task<List<string>> GetCommonGroupsAsync(string contactId)
    {
        contactId = WhatsAppId.EnsureChatSuffix(contactId);

        try
        {
            var json = await page.EvaluateAsync<string>(
                """
                async (contactId) => {
                    let contact = window.Store.Contact.get(contactId);
                    if (!contact) {
                        const wid = window.Store.WidFactory.createWid(contactId);
                        const chatConstructor = window.Store.Contact.getModelsArray().find(c => !c.isGroup).constructor;
                        contact = new chatConstructor({id: wid});
                    }
                    if (contact.commonGroups) {
                        const groups = contact.commonGroups.serialize();
                        return JSON.stringify(groups.map(g => g.id));
                    }
                    const status = await window.Store.findCommonGroups(contact);
                    if (status) {
                        const groups = contact.commonGroups.serialize();
                        return JSON.stringify(groups.map(g => g.id));
                    }
                    return '[]';
                }
                """, contactId);

            return JsonSerializer.Deserialize<List<string>>(json, jsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting common groups: {ContactId}", contactId);
            return [];
        }
    }
}

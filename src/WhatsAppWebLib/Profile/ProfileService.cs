using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace WhatsAppWebLib.Profile;

public class ProfileService(IPage page, HttpClient httpClient, ILogger logger)
{
    // @wwebjs-source Client.getProfilePicUrl -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1559-L1573
    // @wwebjs-source Store.ProfilePic -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Store.js#L80
    // @wwebjs-source Store.WidFactory -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Store.js#L79
    public async Task<string?> GetPicUrlAsync(string contactId)
    {
        contactId = WhatsAppId.EnsureChatSuffix(contactId);

        try
        {
            return await page.EvaluateAsync<string?>(
                """
                async (contactId) => {
                    const chatWid = window.Store.WidFactory.createWid(contactId);
                    const result = await window.Store.ProfilePic.requestProfilePicFromServer(chatWid);
                    return result?.eurl || null;
                }
                """, contactId);
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("ServerStatusCodeError"))
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting profile picture: {ContactId}", contactId);
            return null;
        }
    }

    public async Task<string?> GetPicBase64Async(string contactId)
    {
        var url = await GetPicUrlAsync(contactId);
        if (string.IsNullOrEmpty(url)) return null;

        try
        {
            var bytes = await httpClient.GetByteArrayAsync(url);
            return Convert.ToBase64String(bytes);
        }
        catch
        {
            return null;
        }
    }

    // @wwebjs-source Client.setStatus -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1485-L1491
    public async Task<bool> SetStatusAsync(string status)
    {
        try
        {
            return await page.EvaluateAsync<bool>(
                """
                async (status) => {
                    await window.Store.StatusUtils.setMyStatus(status);
                    return true;
                }
                """, status);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting status: {Status}", status);
            return false;
        }
    }

    // @wwebjs-source Client.setDisplayName -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1405-L1413
    public async Task<bool> SetDisplayNameAsync(string displayName)
    {
        try
        {
            return await page.EvaluateAsync<bool>(
                """
                async (displayName) => {
                    if (!window.Store.Conn.canSetMyPushname()) return false;
                    await window.Store.Settings.setPushname(displayName);
                    return true;
                }
                """, displayName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting display name: {DisplayName}", displayName);
            return false;
        }
    }

    // @wwebjs-source Client.sendPresenceAvailable -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1514-L1519
    public async Task<bool> SendPresenceAvailableAsync()
    {
        try
        {
            return await page.EvaluateAsync<bool>(
                """
                async () => {
                    await window.Store.PresenceUtils.sendPresenceAvailable();
                    return true;
                }
                """);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending available presence");
            return false;
        }
    }

    // @wwebjs-source Client.sendPresenceUnavailable -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1526-L1531
    public async Task<bool> SendPresenceUnavailableAsync()
    {
        try
        {
            return await page.EvaluateAsync<bool>(
                """
                async () => {
                    await window.Store.PresenceUtils.sendPresenceUnavailable();
                    return true;
                }
                """);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending unavailable presence");
            return false;
        }
    }

    // @wwebjs-source Client.deleteProfilePicture -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1576-L1582
    // @wwebjs-source WWebJS.deletePicture -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js
    public async Task<bool> DeleteProfilePictureAsync()
    {
        try
        {
            return await page.EvaluateAsync<bool>(
                """
                async () => {
                    const me = window.Store.User.getMaybeMeUser();
                    return window.WWebJS.deletePicture(me._serialized);
                }
                """);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting profile picture");
            return false;
        }
    }
}

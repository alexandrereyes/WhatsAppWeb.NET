using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace WhatsAppWebLib.Label;

public class LabelService(IPage page, JsonSerializerOptions jsonOptions, ILogger logger)
{
    // @wwebjs-source WWebJS.getLabels -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L854-L857
    // @wwebjs-source Client.getLabels -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L2016-L2022
    // @wwebjs-source WWebJS.getLabelModel -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L847-L852
    public async Task<List<LabelModel>> GetLabelsAsync()
    {
        try
        {
            var jsonStr = await page.EvaluateAsync<string>(
                """
                async () => {
                    const labels = window.WWebJS.getLabels();
                    return JSON.stringify(labels.map(l => ({
                        id: l.id,
                        name: l.name,
                        hexColor: l.hexColor
                    })));
                }
                """);
            return JsonSerializer.Deserialize<List<LabelModel>>(jsonStr, jsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting labels");
            return [];
        }
    }

    // @wwebjs-source Client.getLabelById -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L2083-L2089
    // @wwebjs-source WWebJS.getLabelModel -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L847-L852
    public async Task<LabelModel?> GetLabelByIdAsync(string labelId)
    {
        try
        {
            var jsonStr = await page.EvaluateAsync<string?>(
                """
                async (labelId) => {
                    const label = window.Store.Label.get(labelId);
                    if (!label) return null;
                    const model = window.WWebJS.getLabelModel(label);
                    return JSON.stringify({ id: model.id, name: model.name, hexColor: model.hexColor });
                }
                """, labelId);

            if (string.IsNullOrEmpty(jsonStr)) return null;
            return JsonSerializer.Deserialize<LabelModel>(jsonStr, jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting label by ID: {LabelId}", labelId);
            return null;
        }
    }

    // @wwebjs-source Client.getChatLabels -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L2042-L2048
    // @wwebjs-source WWebJS.getChatLabels -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L867-L870
    public async Task<List<LabelModel>> GetChatLabelsAsync(string chatId)
    {
        chatId = WhatsAppId.EnsureChatSuffix(chatId);

        try
        {
            var jsonStr = await page.EvaluateAsync<string>(
                """
                async (chatId) => {
                    const chat = await window.WWebJS.getChat(chatId);
                    const labelIds = chat?.labels || [];
                    const labels = labelIds
                        .map(id => window.Store.Label.get(id))
                        .filter(Boolean)
                        .map(l => window.WWebJS.getLabelModel(l));
                    return JSON.stringify(labels.map(l => ({ id: l.id, name: l.name, hexColor: l.hexColor })));
                }
                """, chatId);
            return JsonSerializer.Deserialize<List<LabelModel>>(jsonStr, jsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting chat labels: {ChatId}", chatId);
            return [];
        }
    }

    // @wwebjs-source Client.getChatsByLabelId -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L2055-L2065
    public async Task<List<string>> GetChatsByLabelIdAsync(string labelId)
    {
        try
        {
            var jsonStr = await page.EvaluateAsync<string>(
                """
                async (labelId) => {
                    const label = window.Store.Label.get(labelId);
                    if (!label) return '[]';
                    const labelItems = label.labelItemCollection.getModelsArray();
                    return JSON.stringify(labelItems.map(item => item.parentId));
                }
                """, labelId);
            return JsonSerializer.Deserialize<List<string>>(jsonStr, jsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting chats by label ID: {LabelId}", labelId);
            return [];
        }
    }

    // @wwebjs-source Client.addOrRemoveLabels -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L2072-L2101
    public async Task<bool> AddOrRemoveLabelsAsync(int[] labelIds, string[] chatIds)
    {
        try
        {
            var args = new { labelIds, chatIds };
            return await page.EvaluateAsync<bool>(
                """
                async (args) => {
                    const { labelIds, chatIds } = args;
                    const labels = labelIds.map(id => window.Store.Label.get(id)).filter(Boolean);
                    const chats = await Promise.all(chatIds.map(id => window.WWebJS.getChat(id, { getAsModel: false })));
                    await window.Store.Label.addOrRemoveLabels(labels, chats.filter(Boolean));
                    return true;
                }
                """, args);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding/removing labels: {LabelIds} for chats: {ChatIds}", labelIds, chatIds);
            return false;
        }
    }
}

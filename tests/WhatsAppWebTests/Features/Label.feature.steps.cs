using Reqnroll;
using Shouldly;
using WhatsAppWebLib.Label;
using WhatsAppWebTests.Reqnroll;

namespace WhatsAppWebTests.Features;

[Binding]
public class LabelSteps
{
    private List<LabelModel>? _labels;
    private List<LabelModel>? _chatLabels;
    private List<string>? _chatIds;

    [When("I request the label list")]
    public async Task WhenIRequestTheLabelList()
    {
        var client = await ClientHooks.Client;
        _labels = await client.Label.GetLabelsAsync();
    }

    [Then("I should receive a label list or an empty list")]
    public void ThenIShouldReceiveALabelListOrAnEmptyList()
    {
        _labels.ShouldNotBeNull();
    }

    [When("I request the label with ID {string}")]
    public async Task WhenIRequestTheLabelWithId(string labelId)
    {
        var client = await ClientHooks.Client;
        await client.Label.GetLabelByIdAsync(labelId);
    }

    [Then("I should receive the label data or null")]
    public void ThenIShouldReceiveTheLabelDataOrNull()
    {
        // Label may not exist if not WhatsApp Business; we verify no exception was thrown
    }

    [When("I request the labels for the {TestPhone}")]
    public async Task WhenIRequestTheLabelsForThe(TestPhone phone)
    {
        var chatId = TestConfiguration.ResolvePhone(phone);
        var client = await ClientHooks.Client;
        _chatLabels = await client.Label.GetChatLabelsAsync(chatId);
    }

    [Then("I should receive the chat labels or an empty list")]
    public void ThenIShouldReceiveTheChatLabelsOrAnEmptyList()
    {
        _chatLabels.ShouldNotBeNull();
    }

    [When("I request the chats for label {string}")]
    public async Task WhenIRequestTheChatsForLabel(string labelId)
    {
        var client = await ClientHooks.Client;
        _chatIds = await client.Label.GetChatsByLabelIdAsync(labelId);
    }

    [Then("I should receive the label chats or an empty list")]
    public void ThenIShouldReceiveTheLabelChatsOrAnEmptyList()
    {
        _chatIds.ShouldNotBeNull();
    }
}

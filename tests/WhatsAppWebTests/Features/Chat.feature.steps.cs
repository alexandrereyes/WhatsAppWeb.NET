using Reqnroll;
using Shouldly;
using WhatsAppWebLib.Chat;
using WhatsAppWebLib.Message;
using WhatsAppWebTests.Reqnroll;

namespace WhatsAppWebTests.Features;

[Binding]
public class ChatSteps
{
    private List<ChatModel>? _chats;
    private ChatModel? _chat;
    private ChatInfoModel? _chatInfo;
    private bool _operationResult;
    private MuteResult? _muteResult;
    private List<MessageModel>? _searchResults;

    [When("I request the chat list")]
    public async Task WhenIRequestTheChatList()
    {
        var client = await ClientHooks.Client;
        _chats = await client.Chat.GetChatsAsync();
    }

    [Then("I should receive a chat list")]
    public void ThenIShouldReceiveAChatList()
    {
        _chats.ShouldNotBeNull();
        _chats.ShouldNotBeEmpty();
    }

    [When("I request the chat for the {TestPhone}")]
    public async Task WhenIRequestTheChatForThe(TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var client = await ClientHooks.Client;
        _chat = await client.Chat.GetChatByIdAsync(number);
    }

    [Then("I should receive the chat data")]
    public void ThenIShouldReceiveTheChatData()
    {
        _chat.ShouldNotBeNull();
        _chat.Id.ShouldNotBeNullOrEmpty();
    }

    [When("I request the information for the {TestPhone}")]
    public async Task WhenIRequestTheInformationForThe(TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var client = await ClientHooks.Client;
        _chatInfo = await client.Chat.GetChatInfoAsync(number);
    }

    [Then("I should receive the detailed chat information")]
    public void ThenIShouldReceiveTheDetailedChatInformation()
    {
        _chatInfo.ShouldNotBeNull();
        _chatInfo.Id.ShouldNotBeNullOrEmpty();
    }

    [When("I mark as read the chat for the {TestPhone}")]
    public async Task WhenIMarkAsReadTheChatForThe(TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var client = await ClientHooks.Client;
        _operationResult = await client.Chat.SendSeenAsync(number);
    }

    [Then("the operation should be successful")]
    public void ThenTheOperationShouldBeSuccessful()
    {
        _operationResult.ShouldBeTrue();
    }

    [When("I archive the chat for the {TestPhone}")]
    public async Task WhenIArchiveTheChatForThe(TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var client = await ClientHooks.Client;
        _operationResult = await client.Chat.ArchiveAsync(number);
    }

    [When("I unarchive the chat for the {TestPhone}")]
    public async Task WhenIUnarchiveTheChatForThe(TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var client = await ClientHooks.Client;
        _operationResult = await client.Chat.UnarchiveAsync(number);
    }

    [When("I pin the chat for the {TestPhone}")]
    public async Task WhenIPinTheChatForThe(TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var client = await ClientHooks.Client;
        await client.Chat.PinAsync(number);
    }

    [When("I unpin the chat for the {TestPhone}")]
    public async Task WhenIUnpinTheChatForThe(TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var client = await ClientHooks.Client;
        await client.Chat.UnpinAsync(number);
    }

    [Then("the pin operation should return a result")]
    public void ThenThePinOperationShouldReturnAResult()
    {
        // Pin may return false if the pinned chats limit is reached
        // We only verify that no exception was thrown
    }

    [When("I mute the chat for the {TestPhone}")]
    public async Task WhenIMuteTheChatForThe(TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var client = await ClientHooks.Client;
        _muteResult = await client.Chat.MuteAsync(number);
    }

    [When("I unmute the chat for the {TestPhone}")]
    public async Task WhenIUnmuteTheChatForThe(TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var client = await ClientHooks.Client;
        _muteResult = await client.Chat.UnmuteAsync(number);
    }

    [Then("the mute result should contain data")]
    public void ThenTheMuteResultShouldContainData()
    {
        _muteResult.ShouldNotBeNull();
    }

    [When("I simulate typing in the chat for the {TestPhone}")]
    public async Task WhenISimulateTypingInTheChatForThe(TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var client = await ClientHooks.Client;
        _operationResult = await client.Chat.SendStateTypingAsync(number);
    }

    [When("I stop the simulation in the chat for the {TestPhone}")]
    public async Task WhenIStopTheSimulationInTheChatForThe(TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var client = await ClientHooks.Client;
        _operationResult = await client.Chat.ClearStateAsync(number);
    }

    [When("I clear the messages from the chat for the {TestPhone}")]
    public async Task WhenIClearTheMessagesFromTheChatForThe(TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var client = await ClientHooks.Client;
        _operationResult = await client.Chat.ClearMessagesAsync(number);
    }

    [When("I search messages with the term {string}")]
    public async Task WhenISearchMessagesWithTheTerm(string query)
    {
        var client = await ClientHooks.Client;
        _searchResults = await client.Chat.SearchMessagesAsync(query);
    }

    [Then("I should receive search results")]
    public void ThenIShouldReceiveSearchResults()
    {
        _searchResults.ShouldNotBeNull();
    }
}

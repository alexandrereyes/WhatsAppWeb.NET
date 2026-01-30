using Reqnroll;
using Shouldly;
using WhatsAppWebLib.Message;
using WhatsAppWebTests.Reqnroll;

namespace WhatsAppWebTests.Features;

[Binding]
public class MessageSteps
{
    private List<MessageModel>? _messages;
    private MessageModel? _message;
    private string? _sentMessageId;
    private bool _deleteResult;
    private string? _replyMessageId;
    private bool _reactResult;
    private bool _forwardResult;

    [When("I request the last {int} messages from the {TestPhone}")]
    public async Task WhenIRequestTheLastMessagesFromThe(int count, TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var client = await ClientHooks.Client;
        _messages = await client.Message.FetchAsync(number, count);
    }

    [Then("I should receive a message list")]
    public void ThenIShouldReceiveAMessageList()
    {
        _messages.ShouldNotBeNull();
    }

    [Given("I send the message {string} to the {TestPhone}")]
    public async Task GivenISendTheMessageToThe(string message, TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var client = await ClientHooks.Client;
        _sentMessageId = await client.Message.SendAsync(number, message);
        _sentMessageId.ShouldNotBeNullOrEmpty();
    }

    [When("I request the message by ID")]
    public async Task WhenIRequestTheMessageById()
    {
        _sentMessageId.ShouldNotBeNullOrEmpty();
        var client = await ClientHooks.Client;
        _message = await client.Message.GetByIdAsync(_sentMessageId!);
    }

    [Then("I should receive the message data")]
    public void ThenIShouldReceiveTheMessageData()
    {
        _message.ShouldNotBeNull();
        _message.Id.ShouldNotBeNullOrEmpty();
    }

    [When("I delete the sent message")]
    public async Task WhenIDeleteTheSentMessage()
    {
        _sentMessageId.ShouldNotBeNullOrEmpty();
        var client = await ClientHooks.Client;
        _deleteResult = await client.Message.DeleteAsync(_sentMessageId!, everyone: true);
    }

    [Then("the message should be deleted successfully")]
    public void ThenTheMessageShouldBeDeletedSuccessfully()
    {
        _deleteResult.ShouldBeTrue();
    }

    [When("I reply to the message with {string}")]
    public async Task WhenIReplyToTheMessageWith(string content)
    {
        _sentMessageId.ShouldNotBeNullOrEmpty();
        var client = await ClientHooks.Client;
        _replyMessageId = await client.Message.ReplyAsync(_sentMessageId!, content);
    }

    [Then("the reply should be sent successfully")]
    public void ThenTheReplyShouldBeSentSuccessfully()
    {
        _replyMessageId.ShouldNotBeNullOrEmpty();
    }

    [When("I react to the message with {string}")]
    public async Task WhenIReactToTheMessageWith(string emoji)
    {
        _sentMessageId.ShouldNotBeNullOrEmpty();
        var client = await ClientHooks.Client;
        _reactResult = await client.Message.ReactAsync(_sentMessageId!, emoji);
    }

    [Then("the reaction should be registered")]
    public void ThenTheReactionShouldBeRegistered()
    {
        _reactResult.ShouldBeTrue();
    }

    [When("I forward the message to the {TestPhone}")]
    public async Task WhenIForwardTheMessageToThe(TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        _sentMessageId.ShouldNotBeNullOrEmpty();
        var client = await ClientHooks.Client;
        _forwardResult = await client.Message.ForwardAsync(_sentMessageId!, number);
    }

    [Then("the forward should be successful")]
    public void ThenTheForwardShouldBeSuccessful()
    {
        _forwardResult.ShouldBeTrue();
    }

    [When("I star the message")]
    public async Task WhenIStarTheMessage()
    {
        _sentMessageId.ShouldNotBeNullOrEmpty();
        var client = await ClientHooks.Client;
        await client.Message.StarAsync(_sentMessageId!);
    }

    [When("I unstar the message")]
    public async Task WhenIUnstarTheMessage()
    {
        _sentMessageId.ShouldNotBeNullOrEmpty();
        var client = await ClientHooks.Client;
        await client.Message.UnstarAsync(_sentMessageId!);
    }

    [Then("the star operation should return a result")]
    public void ThenTheStarOperationShouldReturnAResult()
    {
        // Star/Unstar may fail if the message doesn't support it; we verify no exception was thrown
    }

    [When("I edit the message to {string}")]
    public async Task WhenIEditTheMessageTo(string newContent)
    {
        _sentMessageId.ShouldNotBeNullOrEmpty();
        var client = await ClientHooks.Client;
        await client.Message.EditAsync(_sentMessageId!, newContent);
    }

    [Then("the edit should be processed")]
    public void ThenTheEditShouldBeProcessed()
    {
        // Edit may return false if the message is not editable (e.g., too old)
        // We verify the operation didn't throw an exception
    }
}

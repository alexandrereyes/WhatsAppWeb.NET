using Reqnroll;
using Shouldly;
using WhatsAppWebLib.Contact;
using WhatsAppWebTests.Reqnroll;

namespace WhatsAppWebTests.Features;

[Binding]
public class ClientSteps
{
    private string? _messageId;
    private NumberIdResult? _numberIdResult;

    [Given("the client is connected to WhatsApp Web")]
    public static async Task GivenTheClientIsConnectedToWhatsAppWeb()
    {
        var client = await ClientHooks.Client;
        client.ShouldNotBeNull();
    }

    [When("I send the message {string} to the {TestPhone}")]
    public async Task WhenISendTheMessageTo(string message, TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var client = await ClientHooks.Client;
        _messageId = await client.Message.SendAsync(number, message);
    }

    [Then("the message should be sent successfully")]
    public void ThenTheMessageShouldBeSentSuccessfully()
    {
        _messageId.ShouldNotBeNullOrEmpty();
    }

    [When("I look up the {TestPhone}")]
    public async Task WhenILookUpThe(TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var client = await ClientHooks.Client;
        _numberIdResult = await client.Contact.GetNumberIdAsync(number);
    }

    [Then("the number should be registered on WhatsApp")]
    public void ThenTheNumberShouldBeRegisteredOnWhatsApp()
    {
        _numberIdResult.ShouldNotBeNull();
        _numberIdResult.Wid.ShouldNotBeNullOrEmpty();
    }
}

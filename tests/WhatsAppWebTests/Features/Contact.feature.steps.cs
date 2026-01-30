using Reqnroll;
using Shouldly;
using WhatsAppWebLib.Contact;
using WhatsAppWebTests.Reqnroll;

namespace WhatsAppWebTests.Features;

[Binding]
public class ContactSteps
{
    private List<ContactModel>? _contacts;
    private ContactModel? _contact;
    private bool _isRegistered;
    private List<ContactModel>? _blockedContacts;
    private List<string>? _commonGroups;

    [When("I request the contact list")]
    public async Task WhenIRequestTheContactList()
    {
        var client = await ClientHooks.Client;
        _contacts = await client.Contact.GetContactsAsync();
    }

    [Then("I should receive a contact list")]
    public void ThenIShouldReceiveAContactList()
    {
        _contacts.ShouldNotBeNull();
        _contacts.ShouldNotBeEmpty();
    }

    [When("I request the contact for the {TestPhone}")]
    public async Task WhenIRequestTheContactForThe(TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var client = await ClientHooks.Client;
        _contact = await client.Contact.GetContactByIdAsync(number);
    }

    [Then("I should receive the contact data")]
    public void ThenIShouldReceiveTheContactData()
    {
        _contact.ShouldNotBeNull();
        _contact.Id.ShouldNotBeNullOrEmpty();
    }

    [When("I check if the {TestPhone} is registered")]
    public async Task WhenICheckIfThePhoneIsRegistered(TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var client = await ClientHooks.Client;
        _isRegistered = await client.Contact.IsRegisteredUserAsync(number);
    }

    [Then("the number should be registered")]
    public void ThenTheNumberShouldBeRegistered()
    {
        _isRegistered.ShouldBeTrue();
    }

    [When("I request the blocked contacts")]
    public async Task WhenIRequestTheBlockedContacts()
    {
        var client = await ClientHooks.Client;
        _blockedContacts = await client.Contact.GetBlockedContactsAsync();
    }

    [Then("I should receive the blocked list or an empty list")]
    public void ThenIShouldReceiveTheBlockedListOrAnEmptyList()
    {
        _blockedContacts.ShouldNotBeNull();
    }

    [When("I request the status of the {TestPhone}")]
    public async Task WhenIRequestTheStatusOfThe(TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var client = await ClientHooks.Client;
        await client.Contact.GetAboutAsync(number);
    }

    [Then("I should receive the status or null")]
    public void ThenIShouldReceiveTheStatusOrNull()
    {
        // Status may be null if privacy settings don't allow it; we verify no exception was thrown
    }

    [When("I request the common groups with the {TestPhone}")]
    public async Task WhenIRequestTheCommonGroupsWithThe(TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var client = await ClientHooks.Client;
        _commonGroups = await client.Contact.GetCommonGroupsAsync(number);
    }

    [Then("I should receive the common groups list or an empty list")]
    public void ThenIShouldReceiveTheCommonGroupsListOrAnEmptyList()
    {
        _commonGroups.ShouldNotBeNull();
    }
}

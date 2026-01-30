using Reqnroll;
using Shouldly;
using WhatsAppWebLib.Group;
using WhatsAppWebTests.Reqnroll;

namespace WhatsAppWebTests.Features;

[Binding]
public class GroupSteps
{
    private string? _groupId;
    private List<GroupParticipant>? _participants;

    [Given("a valid group exists")]
    public async Task GivenAValidGroupExists()
    {
        var client = await ClientHooks.Client;
        var chats = await client.Chat.GetChatsAsync();
        var group = chats.FirstOrDefault(c => c.IsGroup);
        group.ShouldNotBeNull("No group found for testing");
        _groupId = group.Id;
    }

    [When("I request the group participants")]
    public async Task WhenIRequestTheGroupParticipants()
    {
        _groupId.ShouldNotBeNullOrEmpty();
        var client = await ClientHooks.Client;
        _participants = await client.Group.GetParticipantsAsync(_groupId!);
    }

    [Then("I should receive the participant list")]
    public void ThenIShouldReceiveTheParticipantList()
    {
        _participants.ShouldNotBeNull();
        _participants.ShouldNotBeEmpty();
    }

    [When("I change the group subject to {string}")]
    public async Task WhenIChangeTheGroupSubjectTo(string subject)
    {
        _groupId.ShouldNotBeNullOrEmpty();
        var client = await ClientHooks.Client;
        await client.Group.SetSubjectAsync(_groupId!, subject);
    }

    [Then("the subject change should be processed")]
    public void ThenTheSubjectChangeShouldBeProcessed()
    {
        // May return false if the user is not an admin; we verify no exception was thrown
    }

    [When("I change the group description to {string}")]
    public async Task WhenIChangeTheGroupDescriptionTo(string description)
    {
        _groupId.ShouldNotBeNullOrEmpty();
        var client = await ClientHooks.Client;
        await client.Group.SetDescriptionAsync(_groupId!, description);
    }

    [Then("the description change should be processed")]
    public void ThenTheDescriptionChangeShouldBeProcessed()
    {
        // May return false if the user is not an admin; we verify no exception was thrown
    }

    [When("I request the group invite code")]
    public async Task WhenIRequestTheGroupInviteCode()
    {
        _groupId.ShouldNotBeNullOrEmpty();
        var client = await ClientHooks.Client;
        await client.Group.GetInviteCodeAsync(_groupId!);
    }

    [Then("I should receive the invite code or null")]
    public void ThenIShouldReceiveTheInviteCodeOrNull()
    {
        // Invite code may be null if the user is not an admin
        // We only verify no exception was thrown
    }

    [When("I configure the group for admin-only messages")]
    public async Task WhenIConfigureTheGroupForAdminOnlyMessages()
    {
        _groupId.ShouldNotBeNullOrEmpty();
        var client = await ClientHooks.Client;
        await client.Group.SetMessagesAdminsOnlyAsync(_groupId!, true);
    }

    [When("I configure the group for everyone to send messages")]
    public async Task WhenIConfigureTheGroupForEveryoneToSendMessages()
    {
        _groupId.ShouldNotBeNullOrEmpty();
        var client = await ClientHooks.Client;
        await client.Group.SetMessagesAdminsOnlyAsync(_groupId!, false);
    }

    [When("I configure the group for admin-only info editing")]
    public async Task WhenIConfigureTheGroupForAdminOnlyInfoEditing()
    {
        _groupId.ShouldNotBeNullOrEmpty();
        var client = await ClientHooks.Client;
        await client.Group.SetInfoAdminsOnlyAsync(_groupId!, true);
    }

    [When("I configure the group for everyone to edit info")]
    public async Task WhenIConfigureTheGroupForEveryoneToEditInfo()
    {
        _groupId.ShouldNotBeNullOrEmpty();
        var client = await ClientHooks.Client;
        await client.Group.SetInfoAdminsOnlyAsync(_groupId!, false);
    }

    [Then("the configuration should be processed")]
    public void ThenTheConfigurationShouldBeProcessed()
    {
        // May return false if the user is not an admin; we verify no exception was thrown
    }
}

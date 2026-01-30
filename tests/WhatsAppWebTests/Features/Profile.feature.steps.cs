using Reqnroll;
using Shouldly;
using WhatsAppWebTests.Reqnroll;

namespace WhatsAppWebTests.Features;

[Binding]
public class ProfileSteps
{
    private string? _profilePicUrl;
    private string? _profilePicBase64;
    private bool _operationResult;

    [When("I request the profile picture of the {TestPhone}")]
    public async Task WhenIRequestTheProfilePictureOfThe(TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var client = await ClientHooks.Client;
        _profilePicUrl = await client.Profile.GetPicUrlAsync(number);
    }

    [Then("I should receive the photo URL or null")]
    public void ThenIShouldReceiveThePhotoUrlOrNull()
    {
        (_profilePicUrl == null || _profilePicUrl.StartsWith("http")).ShouldBeTrue();
    }

    [When("I request the profile picture in base64 of the {TestPhone}")]
    public async Task WhenIRequestTheProfilePictureInBase64OfThe(TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var client = await ClientHooks.Client;
        _profilePicBase64 = await client.Profile.GetPicBase64Async(number);
    }

    [Then("I should receive the base64 photo or null")]
    public void ThenIShouldReceiveTheBase64PhotoOrNull()
    {
        if (_profilePicBase64 != null)
        {
            _profilePicBase64.Length.ShouldBeGreaterThan(0);
        }
    }

    [When("I set the status to {string}")]
    public async Task WhenISetTheStatusTo(string status)
    {
        var client = await ClientHooks.Client;
        _operationResult = await client.Profile.SetStatusAsync(status);
    }

    [Then("the status setting should be processed")]
    public void ThenTheStatusSettingShouldBeProcessed()
    {
        _operationResult.ShouldBeTrue();
    }

    [When("I set the display name to {string}")]
    public async Task WhenISetTheDisplayNameTo(string displayName)
    {
        var client = await ClientHooks.Client;
        _operationResult = await client.Profile.SetDisplayNameAsync(displayName);
    }

    [Then("the name setting should be processed")]
    public void ThenTheNameSettingShouldBeProcessed()
    {
        _operationResult.ShouldBeTrue();
    }

    [When("I send available presence")]
    public async Task WhenISendAvailablePresence()
    {
        var client = await ClientHooks.Client;
        _operationResult = await client.Profile.SendPresenceAvailableAsync();
    }

    [When("I send unavailable presence")]
    public async Task WhenISendUnavailablePresence()
    {
        var client = await ClientHooks.Client;
        _operationResult = await client.Profile.SendPresenceUnavailableAsync();
    }

    [Then("the presence should be registered")]
    public void ThenThePresenceShouldBeRegistered()
    {
        _operationResult.ShouldBeTrue();
    }
}

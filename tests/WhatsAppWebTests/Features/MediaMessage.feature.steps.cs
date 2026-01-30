using Reqnroll;
using Shouldly;
using WhatsAppWebLib.Message;
using WhatsAppWebTests.Reqnroll;

namespace WhatsAppWebTests.Features;

[Binding]
public class MediaMessageSteps
{
    private string? _mediaMessageId;
    private MessageMedia? _media;

    private static string GetTestDataPath(string fileName)
    {
        var baseDir = AppContext.BaseDirectory;
        return Path.Combine(baseDir, "TestData", fileName);
    }

    // === When steps: sending media ===

    [When("I send the image {string} to the {TestPhone}")]
    public async Task WhenISendTheImageToThe(string fileName, TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var filePath = GetTestDataPath(fileName);
        var media = MessageMedia.FromFilePath(filePath);
        var client = await ClientHooks.Client;
        _mediaMessageId = await client.Message.SendAsync(number, media);
    }

    [When("I send the image {string} with caption {string} to the {TestPhone}")]
    public async Task WhenISendTheImageWithCaptionToThe(string fileName, string caption, TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var filePath = GetTestDataPath(fileName);
        var media = MessageMedia.FromFilePath(filePath);
        var client = await ClientHooks.Client;
        _mediaMessageId = await client.Message.SendAsync(number, caption, new MessageSendOptions
        {
            Media = media
        });
    }

    [When("I send the document {string} to the {TestPhone}")]
    public async Task WhenISendTheDocumentToThe(string fileName, TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var filePath = GetTestDataPath(fileName);
        var media = MessageMedia.FromFilePath(filePath);
        var client = await ClientHooks.Client;
        _mediaMessageId = await client.Message.SendAsync(number, string.Empty, new MessageSendOptions
        {
            Media = media,
            SendMediaAsDocument = true
        });
    }

    [When("I send the document {string} with caption {string} to the {TestPhone}")]
    public async Task WhenISendTheDocumentWithCaptionToThe(string fileName, string caption, TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var filePath = GetTestDataPath(fileName);
        var media = MessageMedia.FromFilePath(filePath);
        var client = await ClientHooks.Client;
        _mediaMessageId = await client.Message.SendAsync(number, caption, new MessageSendOptions
        {
            Media = media,
            SendMediaAsDocument = true
        });
    }

    [When("I send the audio {string} to the {TestPhone}")]
    public async Task WhenISendTheAudioToThe(string fileName, TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var filePath = GetTestDataPath(fileName);
        var media = MessageMedia.FromFilePath(filePath);
        var client = await ClientHooks.Client;
        _mediaMessageId = await client.Message.SendAsync(number, media);
    }

    [When("I send the audio {string} as voice message to the {TestPhone}")]
    public async Task WhenISendTheAudioAsVoiceMessageToThe(string fileName, TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var filePath = GetTestDataPath(fileName);
        var media = MessageMedia.FromFilePath(filePath);
        var client = await ClientHooks.Client;
        _mediaMessageId = await client.Message.SendAsync(number, string.Empty, new MessageSendOptions
        {
            Media = media,
            SendAudioAsVoice = true
        });
    }

    [When("I send the video {string} to the {TestPhone}")]
    public async Task WhenISendTheVideoToThe(string fileName, TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var filePath = GetTestDataPath(fileName);
        var media = MessageMedia.FromFilePath(filePath);
        var client = await ClientHooks.Client;
        _mediaMessageId = await client.Message.SendAsync(number, media);
    }

    [When("I send the video {string} with caption {string} to the {TestPhone}")]
    public async Task WhenISendTheVideoWithCaptionToThe(string fileName, string caption, TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var filePath = GetTestDataPath(fileName);
        var media = MessageMedia.FromFilePath(filePath);
        var client = await ClientHooks.Client;
        _mediaMessageId = await client.Message.SendAsync(number, caption, new MessageSendOptions
        {
            Media = media
        });
    }

    [When("I send a PNG image from raw bytes to the {TestPhone}")]
    public async Task WhenISendAPngImageFromRawBytesToThe(TestPhone phone)
    {
        var number = TestConfiguration.ResolvePhone(phone);
        var filePath = GetTestDataPath("test-image.png");
        var bytes = await File.ReadAllBytesAsync(filePath);
        var media = MessageMedia.FromBytes(bytes, "image/png", "raw-bytes-image.png");
        var client = await ClientHooks.Client;
        _mediaMessageId = await client.Message.SendAsync(number, media);
    }

    // === Then steps: media message assertions ===

    [Then("the media message should be sent successfully")]
    public void ThenTheMediaMessageShouldBeSentSuccessfully()
    {
        _mediaMessageId.ShouldNotBeNullOrEmpty("Expected a message ID after sending media, but got null/empty.");
    }

    // === MessageMedia factory method tests ===

    [Given("the test file {string} exists")]
    public void GivenTheTestFileExists(string fileName)
    {
        var filePath = GetTestDataPath(fileName);
        File.Exists(filePath).ShouldBeTrue($"Test file not found: {filePath}");
    }

    [When("I create a MessageMedia from the file path")]
    public void WhenICreateAMessageMediaFromTheFilePath()
    {
        var filePath = GetTestDataPath("test-image.png");
        _media = MessageMedia.FromFilePath(filePath);
    }

    [When("I create a MessageMedia from the URL {string}")]
    public async Task WhenICreateAMessageMediaFromTheUrl(string url)
    {
        _media = await MessageMedia.FromUrlAsync(url);
    }

    [Then("the media should have MIME type {string}")]
    public void ThenTheMediaShouldHaveMimeType(string expectedMimeType)
    {
        _media.ShouldNotBeNull();
        _media.MimeType.ShouldBe(expectedMimeType);
    }

    [Then("the media should have a non-empty data payload")]
    public void ThenTheMediaShouldHaveANonEmptyDataPayload()
    {
        _media.ShouldNotBeNull();
        _media.Data.ShouldNotBeNullOrEmpty();
    }

    [Then("the media should have file name {string}")]
    public void ThenTheMediaShouldHaveFileName(string expectedFileName)
    {
        _media.ShouldNotBeNull();
        _media.FileName.ShouldBe(expectedFileName);
    }
}

Feature: Media Message
    As a developer
    I want to send media messages via WhatsApp Web
    To share images, documents and other files

    Background:
        Given the client is connected to WhatsApp Web

    Scenario: Send an image from a local file
        When I send the image "test-image.png" to the primary phone
        Then the media message should be sent successfully

    Scenario: Send an image with a caption
        When I send the image "test-image.png" with caption "Test image caption" to the primary phone
        Then the media message should be sent successfully

    Scenario: Send a document from a local file
        When I send the document "test-document.txt" to the primary phone
        Then the media message should be sent successfully

    Scenario: Send a document with a caption
        When I send the document "test-document.txt" with caption "Test document caption" to the primary phone
        Then the media message should be sent successfully

    Scenario: Send media from raw bytes
        When I send a PNG image from raw bytes to the primary phone
        Then the media message should be sent successfully

    Scenario: Send an audio file
        When I send the audio "test-audio.mp3" to the primary phone
        Then the media message should be sent successfully

    Scenario: Send an OGG audio as document
        When I send the document "test-audio.ogg" to the primary phone
        Then the media message should be sent successfully

    Scenario: Send an audio file as voice message
        When I send the audio "test-audio.ogg" as voice message to the primary phone
        Then the media message should be sent successfully

    Scenario: Send a video file
        When I send the video "test-video.mp4" to the primary phone
        Then the media message should be sent successfully

    Scenario: Send a video file with a caption
        When I send the video "test-video.mp4" with caption "Test video caption" to the primary phone
        Then the media message should be sent successfully

    Scenario: Create MessageMedia from file path
        Given the test file "test-image.png" exists
        When I create a MessageMedia from the file path
        Then the media should have MIME type "image/png"
        And the media should have a non-empty data payload
        And the media should have file name "test-image.png"

    Scenario: Create MessageMedia from URL
        When I create a MessageMedia from the URL "https://www.google.com/images/branding/googlelogo/1x/googlelogo_color_272x92dp.png"
        Then the media should have MIME type "image/png"
        And the media should have a non-empty data payload

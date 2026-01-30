Feature: Profile
    As a developer
    I want to manage WhatsApp Web profile and presence
    To display avatars and control online status

    Background:
        Given the client is connected to WhatsApp Web

    Scenario: Get profile picture URL
        When I request the profile picture of the primary phone
        Then I should receive the photo URL or null

    Scenario: Get profile picture in base64
        When I request the profile picture in base64 of the primary phone
        Then I should receive the base64 photo or null

    Scenario: Set profile status
        When I set the status to "Testing WhatsAppWebLib"
        Then the status setting should be processed

    Scenario: Set display name
        When I set the display name to "WhatsApp Bot Test"
        Then the name setting should be processed

    Scenario: Send online presence
        When I send available presence
        Then the presence should be registered

    Scenario: Send offline presence
        When I send unavailable presence
        Then the presence should be registered

Feature: Group
    As a developer
    I want to manage WhatsApp Web groups
    To get participant and configuration information

    Background:
        Given the client is connected to WhatsApp Web

    Scenario: Get group participants
        Given a valid group exists
        When I request the group participants
        Then I should receive the participant list

    Scenario: Change group subject
        Given a valid group exists
        When I change the group subject to "Test WhatsAppWebLib"
        Then the subject change should be processed

    Scenario: Change group description
        Given a valid group exists
        When I change the group description to "Test description"
        Then the description change should be processed

    Scenario: Get group invite code
        Given a valid group exists
        When I request the group invite code
        Then I should receive the invite code or null

    Scenario: Configure group for admin-only messages
        Given a valid group exists
        When I configure the group for admin-only messages
        Then the configuration should be processed
        When I configure the group for everyone to send messages
        Then the configuration should be processed

    Scenario: Configure group for admin-only info editing
        Given a valid group exists
        When I configure the group for admin-only info editing
        Then the configuration should be processed
        When I configure the group for everyone to edit info
        Then the configuration should be processed

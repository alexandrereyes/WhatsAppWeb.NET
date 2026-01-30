Feature: Chat
    As a developer
    I want to manage WhatsApp Web chats
    To get conversation information

    Background:
        Given the client is connected to WhatsApp Web

    Scenario: Get chat list
        When I request the chat list
        Then I should receive a chat list

    Scenario: Get a specific chat
        When I request the chat for the primary phone
        Then I should receive the chat data

    Scenario: Get detailed chat information
        When I request the information for the primary phone
        Then I should receive the detailed chat information

    Scenario: Mark chat as read
        When I mark as read the chat for the primary phone
        Then the operation should be successful

    Scenario: Archive and unarchive a chat
        When I archive the chat for the primary phone
        Then the operation should be successful
        When I unarchive the chat for the primary phone
        Then the operation should be successful

    Scenario: Pin and unpin a chat
        When I pin the chat for the primary phone
        Then the pin operation should return a result
        When I unpin the chat for the primary phone
        Then the pin operation should return a result

    Scenario: Mute and unmute a chat
        When I mute the chat for the primary phone
        Then the mute result should contain data
        When I unmute the chat for the primary phone
        Then the mute result should contain data

    Scenario: Simulate typing in chat
        When I simulate typing in the chat for the primary phone
        Then the operation should be successful
        When I stop the simulation in the chat for the primary phone
        Then the operation should be successful

    Scenario: Clear messages from a chat
        When I clear the messages from the chat for the primary phone
        Then the operation should be successful

    Scenario: Search messages
        When I search messages with the term "Blah"
        Then I should receive search results

Feature: Message
    As a developer
    I want to manage WhatsApp Web messages
    To send, fetch and delete messages

    Background:
        Given the client is connected to WhatsApp Web

    Scenario: Get messages from a chat
        When I request the last 10 messages from the primary phone
        Then I should receive a message list

    Scenario: Get a specific message
        Given I send the message "Blah" to the primary phone
        When I request the message by ID
        Then I should receive the message data

    Scenario: Delete a message
        Given I send the message "Blah" to the primary phone
        When I delete the sent message
        Then the message should be deleted successfully

    Scenario: Reply to a message
        Given I send the message "Original message for reply" to the primary phone
        When I reply to the message with "This is a reply"
        Then the reply should be sent successfully

    Scenario: React to a message
        Given I send the message "Message for reaction" to the primary phone
        When I react to the message with "👍"
        Then the reaction should be registered

    Scenario: Forward a message
        Given I send the message "Message to forward" to the primary phone
        When I forward the message to the primary phone
        Then the forward should be successful

    Scenario: Star and unstar a message
        Given I send the message "Message to star" to the primary phone
        When I star the message
        Then the star operation should return a result
        When I unstar the message
        Then the star operation should return a result

    Scenario: Edit a message
        Given I send the message "Message to edit" to the primary phone
        When I edit the message to "Edited message"
        Then the edit should be processed

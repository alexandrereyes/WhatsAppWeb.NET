Feature: Label
    As a developer
    I want to manage WhatsApp Business labels
    To organize conversations

    Background:
        Given the client is connected to WhatsApp Web

    Scenario: Get labels
        When I request the label list
        Then I should receive a label list or an empty list

    Scenario: Get label by ID
        When I request the label with ID "1"
        Then I should receive the label data or null

    Scenario: Get labels for a chat
        When I request the labels for the primary phone
        Then I should receive the chat labels or an empty list

    Scenario: Get chats by label
        When I request the chats for label "1"
        Then I should receive the label chats or an empty list

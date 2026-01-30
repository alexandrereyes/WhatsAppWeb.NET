Feature: Client
    As a developer
    I want to use the WhatsApp Web Client
    To send automated messages

    Background:
        Given the client is connected to WhatsApp Web

    Scenario: Send a message to a contact
        When I send the message "Blah" to the primary phone
        Then the message should be sent successfully

    Scenario: Verify if a number is registered on WhatsApp
        When I look up the primary phone
        Then the number should be registered on WhatsApp

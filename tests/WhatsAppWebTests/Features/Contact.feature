Feature: Contact
    As a developer
    I want to manage WhatsApp Web contacts
    To get contact information

    Background:
        Given the client is connected to WhatsApp Web

    Scenario: Get contact list
        When I request the contact list
        Then I should receive a contact list

    Scenario: Get a specific contact
        When I request the contact for the primary phone
        Then I should receive the contact data

    Scenario: Verify if a number is registered
        When I check if the primary phone is registered
        Then the number should be registered

    Scenario: Get blocked contacts
        When I request the blocked contacts
        Then I should receive the blocked list or an empty list

    Scenario: Get a contact's status
        When I request the status of the primary phone
        Then I should receive the status or null

    Scenario: Get common groups with a contact
        When I request the common groups with the primary phone
        Then I should receive the common groups list or an empty list

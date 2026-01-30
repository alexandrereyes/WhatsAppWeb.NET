@infrastructure

Feature: Infrastructure Verification
    To ensure the environment is correctly configured
    As a developer
    I want to verify that logging works in xUnit output

    Scenario: ILogger writes to xUnit console
        When I log a message with text "Test message to validate ILogger"
        Then the log message should appear in the test output

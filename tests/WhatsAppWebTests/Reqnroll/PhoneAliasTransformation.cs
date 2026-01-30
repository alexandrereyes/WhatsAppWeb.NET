using Reqnroll;

namespace WhatsAppWebTests.Reqnroll;

/// <summary>
/// ReqnRoll step argument transformation that converts phone alias text
/// from Gherkin features into <see cref="TestPhone"/> enum values.
/// This enables writing readable feature files without hardcoded phone numbers.
///
/// Usage in Gherkin (without quotes):
///   When I request the chat for the primary phone
/// </summary>
[Binding]
public static class PhoneAliasTransformation
{
    [StepArgumentTransformation(@"primary phone")]
    public static TestPhone PrimaryPhone() => TestPhone.Principal;
}

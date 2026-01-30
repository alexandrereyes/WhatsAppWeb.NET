namespace WhatsAppWebTests.Reqnroll;

/// <summary>
/// Phone alias used in Gherkin features to avoid hardcoding real phone numbers.
/// The actual number is resolved from the TARGET_TEST_PHONE environment variable at runtime.
/// </summary>
public enum TestPhone
{
    /// <summary>Target phone for test operations (TARGET_TEST_PHONE).</summary>
    Principal
}

/// <summary>
/// Reads test phone numbers from environment variables.
/// Required variables:
///   - PHONE_NUMBER: phone number for pairing/authentication
///   - TARGET_TEST_PHONE: target phone for test operations
/// </summary>
public static class TestConfiguration
{
    private const string PhoneNumberVar = "PHONE_NUMBER";
    private const string TargetTestPhoneVar = "TARGET_TEST_PHONE";

    /// <summary>Phone number used for WhatsApp pairing/authentication.</summary>
    public static string PhoneNumber => GetRequired(PhoneNumberVar);

    /// <summary>Target phone number for test operations.</summary>
    public static string TargetTestPhone => GetRequired(TargetTestPhoneVar);

    /// <summary>
    /// Resolves a <see cref="TestPhone"/> alias to the actual phone number from environment variables.
    /// </summary>
    public static string ResolvePhone(TestPhone phone) => phone switch
    {
        TestPhone.Principal => TargetTestPhone,
        _ => throw new ArgumentOutOfRangeException(nameof(phone), phone, "Unknown phone alias.")
    };

    private static string GetRequired(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"Environment variable '{variableName}' not configured. " +
                "Configure the environment variables before running the tests. " +
                "See the .env.example file in the project root.");

        return value;
    }
}

# WhatsAppWeb.NET

Port of [whatsapp-web.js](https://github.com/pedroslopez/whatsapp-web.js/) (Node.js) to C# / .NET.

The original project, maintained by Pedro S. Lopez, is a WhatsApp client library that operates via WhatsApp Web using browser automation. This version reimplements the same approach in C# using Microsoft Playwright instead of Puppeteer.

## Language Convention

All content in this project **must** be written in **English (en-US)**. This includes:

- Source code (identifiers, comments, log messages, exception messages)
- Documentation (markdown files, XML doc comments)
- Test scenarios (Gherkin feature files, step definitions)
- String literals (error messages, user-facing text)
- Commit messages and PR descriptions

No Portuguese (pt-BR) or any other language should be used anywhere in the codebase.

---

## Project Phase

This project is in **active development phase**. Breaking changes to the public API are acceptable and encouraged when they improve the architecture, readability, or maintainability of the code. Prefer robust and correct changes over premature backward compatibility.

## Upstream Code Tracking (whatsapp-web.js)

This project contains inline JavaScript snippets (strings executed via Playwright `EvaluateAsync`) that were ported from the original [whatsapp-web.js](https://github.com/pedroslopez/whatsapp-web.js/) repository by Pedro S. Lopez.

To maintain traceability between our code and the upstream, each inline JS block has comments with the `@wwebjs-source` marker pointing to the exact file and lines in the original repository. Example:

```csharp
// @wwebjs-source WWebJS.getChats -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L621-L625
// @wwebjs-source Client.getChats -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/Client.js#L1161-L1167
public async Task<List<ChatModel>> GetChatsAsync()
```

### Rules

- Every inline JS block ported from upstream **must** have at least one `@wwebjs-source` comment with the GitHub permalink
- The format is: `// @wwebjs-source FunctionName -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/...#LXXX-LYYY`
- When adding new JS snippets ported from upstream, always include the marker
- To list all mapped points: `grep -rn "@wwebjs-source" WhatsAppWebLib/`

### Purpose

These markers enable creating a checking agent that compares our code with the upstream and identifies changes that may require updating our port (renamed functions, changed parameters, modified logic, etc.).

---

## Running Tests

Tests are BDD (ReqnRoll/Gherkin) and most require an authenticated WhatsApp Web session. See the [README](README.md) for the full getting started guide (configure `.env`, build, pair).

### Commands

```bash
# Build first (avoids dotnet test timeout)
dotnet build WhatsAppWeb.slnx -v q

# Run all tests
dotnet test WhatsAppWeb.slnx --no-build

# Infrastructure tests only (do not require WhatsApp authentication)
dotnet test WhatsAppWeb.slnx --no-build --filter "Category=infrastructure"

# By scenario name (substring match)
dotnet test WhatsAppWeb.slnx --no-build --filter "DisplayName~chat list"

# List available tests
dotnet test WhatsAppWeb.slnx --no-build --list-tests
```

### WhatsApp Authentication

When running tests that require the WhatsApp client, the test runner displays the pairing code in the output:

```
[xUnit.net 00:00:10.23] WhatsAppWebTests: [Warning] ClientHooks: === PAIRING CODE: XXXX1234 ===
```

The code must be entered in the WhatsApp mobile app under **Linked Devices > Link a Device > Enter code**. The authentication timeout is 1 minute.

### Test Logging

`ILogger` logs appear in the test runner output via `TestOutputLoggerProvider`:
- **During scenarios**: logs go to `ITestOutputHelper` (test output)
- **Before scenarios** (e.g., client initialization): logs go to `TestContext.Current.SendDiagnosticMessage()` - requires `"diagnosticMessages": true` in `xunit.runner.json`

### Troubleshooting

| Problem | Solution |
|---------|----------|
| Pairing code does not appear | Check `"diagnosticMessages": true` in `xunit.runner.json` |
| Authentication timeout | Enter the code on the mobile device within 1 minute |
| Tests hang without output | Always run `dotnet build` separately before `dotnet test --no-build` |
| Infrastructure test fails | Check that `TestOutputLoggerProvider` is configured in `Hooks.cs` |

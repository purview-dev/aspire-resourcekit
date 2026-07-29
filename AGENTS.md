# AGENTS.md

## Purpose

This repository builds **Purview.Aspire.ResourceKit**: a .NET Aspire-focused toolkit that combines runtime abstractions with source generation so AppHost resource composition is strongly typed, testable, and maintainable.

At a high level:

- `src/src/ResourceKit` contains runtime APIs used by host/resource kit classes.
- `src/src/SourceGeneration` contains the Roslyn incremental generator and generated attribute contracts.
- `src/tests/*` validates runtime behavior and generated output.

## Technology and tooling context

This is a .NET repository centered on Aspire and source generation.

- Uses the **Purview DotNet Project SDK** (`Purview.DotNetProjectSdk`) for project defaults and conventions.
- Uses centrally-managed package versions (`Directory.Packages.props`).
- Uses **TUnit** and **TUnit.Mocks** for testing.
- Uses `Microsoft.Testing.Platform` via package-managed tooling.

For additional agent capabilities, conventions, and reusable workflows, use the repository’s `.agents/` folder.
Do not assume this file contains every available helper; consult `.agents/` as part of planning and execution.

## Purview DotNet Project SDK guidance

When adding or modifying projects in this repository:

- Follow the Purview DotNet Project SDK conventions already used in `src/Directory.Build.props` and `src/Directory.Build.targets`.
- Preserve SDK-driven namespace and project structure conventions.
- Prefer SDK/Directory.Build-driven package and build configuration over ad-hoc per-project customization.
- Avoid introducing manual structure overrides unless there is a clear, documented reason.

In practice, this means new code should align to existing namespace prefixes, folder layout, and central package management patterns already established by the SDK.

## Testing requirements (mandatory)

Work is **not complete** until relevant tests pass.

### Test framework

- Use **TUnit** and **TUnit.Mocks**.
- Do **not** introduce NUnit, xUnit, or MSTest patterns/packages.

### Test structure and style

All tests must use AAA with explicit comments:

- `// Arrange`
- `// Act`
- `// Assert`

### Naming conventions

Use naming that mirrors production structure:

- Test namespace should match the target namespace shape (same hierarchy intent).
- Test class naming: `{ClassUnderTest}Tests`
- Test method naming: `{SubjectOrMethodUnderTest}_{Scenario}_{Expectation}`

### Cancellation token rule

If any method invoked in the test body supports a `CancellationToken`, pass a token and make it the **final argument**.

- Prefer the real `CancellationToken` even when cancellation behavior is not under test, giving long running tests the opportunity to be cancelled.
- Use explicit token sources when cancellation semantics are under test.

## Completion criteria

Before considering a change done:

1. Relevant tests are updated/added using the conventions above.
2. Test runs pass for the impacted scope.
3. New/updated docs stay consistent with this file and the `.agents/` guidance.

---
name: dotnet-tunit
description: Use when creating or refactoring .NET tests with TUnit conventions (AAA pattern, naming, async assertions, and CancellationToken usage).
category: dotnet
roles:
  - dotnet
  - dotnet-tunit
  - coding
tags:
  - dotnet
  - csharp
  - tunit
  - tests
---

# dotnet-tunit Skill

Use this skill when creating or refactoring tests in a .NET codebase that uses TUnit.

## Authoring and refactoring rules (mandatory)

- Always use TUnit (`TUnit.Core`, `TUnit.Assertions`).
- Do not introduce xUnit, NUnit, MSTest, or FluentAssertions unless explicitly requested.
- Test methods must have `[Test]`.
- Assertion calls must be awaited.
- Prefer `await Assert.That(actual).IsEqualTo(expected);`.

### Arrange / Act / Assert structure

- Always use Arrange / Act / Assert.
- Always include explicit section comments in the test body:
  - `// Arrange`
  - `// Act`
  - `// Assert`

### Naming conventions

- Test classes must be named `{ModuleOrClassBeingTested}Tests`.
- Test classes must be in the same namespace as the module/class being tested (identical namespace).
- Test methods must follow `{SubjectUnderTest}_{Scenario}_{Expectation}`.
  - `SubjectUnderTest` is usually the method name.
  - Use `ctor` for constructor-focused tests when appropriate.

### CancellationToken rule

- Always include `CancellationToken cancellationToken` as the last parameter when any called method in the test body supports a `CancellationToken` parameter.
- Pass that token through in the call under test (for example: `var result = await sut.ProcessAsync(cancellationToken);`).

## Refactoring expectation

- When refactoring existing tests, bring them into compliance with all rules above (structure, naming, and cancellation-token usage), while preserving test intent.

## Execution reminder

- Run relevant tests before completion.
- For execution/filtering details, use `../tunit-test-runner/SKILL.md`.

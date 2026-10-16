# AGENTS.md

## Purpose

This repository builds **Purview.Aspire.ResourceKit**: a .NET Aspire-focused toolkit that combines runtime abstractions with source generation so AppHost resource composition is strongly typed, testable, and maintainable.

At a high level:

- `src/src/ResourceKit` contains runtime APIs used by host/resource kit classes.
- `src/src/SourceGeneration` contains the Roslyn incremental generator and generated attribute contracts.
- `src/tests/*` validates runtime behavior and generated output.

## Technology and tooling context

This is a .NET repository centered on Aspire and source generation.

- Uses the **Purview DotNet Project SDK** (`Purview.BuildSdk`) for project defaults and conventions.
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

## Agent skills

Authored skills live in the repository-root `skills/` folder. **Nothing packs them into a NuGet
package.** `Purview.BuildSdk` 1.0.6 removed skill packaging outright — the skills CLI clones the
*git* repository and only discovers a root `skills/` folder, so that is the one delivery route.
`PurviewSkillsToPack`, the `IncludeRepositorySkillsInPackage` shim in `src/Directory.Build.targets`,
and the `.agents/**.*` entry in `.config/purview-build.json` went with it.

`.agents/skills/` is the CLI's install location for skills from **other** repositories, and is
gitignored. This repository's own skills are never installed into it — that would leave two copies to
drift apart. Read them at their authored path, `skills/<name>/SKILL.md`.

```bash
npx skills add purview-dev/aspire-resourcekit
```

| Skill | Answers |
| --- | --- |
| `skills/purview-resourcekit-authoring/` | declaring a `[HostKit]` and resource kits, attribute styles, the Build/Configure lifecycle, project resources |
| `skills/purview-resourcekit-options/` | generated options, extending them, `IsEnabled` versus `IsResourceEnabled`, `OptionsHelper` |
| `skills/purview-resourcekit-testing/` | TUnit.Aspire fixtures, overriding options from a test, running the tiers |
| `skills/purview-resourcekit-diagnostics/` | what an `SG00xx` means and how to fix it |
| `skills/purview-resourcekit-migration/` | converting an existing inline AppHost into ResourceKit composition |

Every skill carries the `purview-resourcekit` prefix. Skills install into one flat directory per
agent, shared with every skill a developer has installed from anywhere, so an unprefixed name
collides and the loser is whichever installed second.

Six skills were **removed** in the 1.0.6 migration: `changesets-prerelease`, `dotnet-tunit`,
`git-conventional-commits`, `lefthook-integration`, `tunit-filtering` and `tunit-test-runner`. None
of them described ResourceKit, all of them carried exactly the kind of generic name that collides in
a shared install directory, and the TUnit ones duplicate `purview-buildsdk-testing`
(`npx skills add purview-dev/build-sdk`). Guidance about working *in this repository* belongs in this
file, not in a skill — a skill here ships to somebody else.

To add a skill: create `skills/<name>/SKILL.md` with spec-legal frontmatter. There is no per-project
registration any more.

`src/tests/Repository.UnitTests/SkillTests.cs` validates every skill on every test run, through
`Purview.Skills.Testing`'s rule set (the Agent Skills specification: frontmatter fields, name and
description constraints, bundled links, body size). The `[InheritsTests]` attribute on that class is
load-bearing — TUnit does not walk base types during discovery, so without it the run is green and
nothing is checked.

A relative link from one `SKILL.md` into another skill's folder is a rule violation: only a skill's
own folder is installed with it, so such a link works here and nowhere else. Name the other skill in
prose instead, and keep deep detail in the skill's own `references/`.

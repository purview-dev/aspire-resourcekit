# AGENTS.md

## Purpose

This repository builds **Purview.Aspire.ResourceKit**: a .NET Aspire-focused toolkit that combines runtime abstractions with source generation so AppHost resource composition is strongly typed, testable, and maintainable.

At a high level:

- `src/src/ResourceKit` contains runtime APIs used by host/resource kit classes.
- `src/src/SourceGeneration` contains the Roslyn incremental generator and generated attribute contracts.
- `src/tests/*` validates runtime behavior and generated output.

## Technology and tooling context

This is a .NET repository centered on Aspire and source generation.

- **.NET 10 is the floor; .NET 11 is the newest leg**: `src/Versions.props` declares
  `DefaultTargetFramework` (`net10.0`) and `LatestTargetFramework` (`net11.0`).
  `Purview.BuildSdk` 2.x resolves the build from its `Current` set (`net10.0;net11.0`), and the package
  ships `lib/net10.0` and `lib/net11.0`. The .NET 10 leg declares the lowest-compatible
  `Microsoft.Extensions.*` floor; do not raise it to the 11.x band just because the net11.0 leg exists.
- **.NET 11 opt-ins**: `MemorySafetyRules=2` (the C# 15 updated memory-safety rules) is a BuildSdk
  default for `net11.0` targets. Runtime Async is **not** a default in BuildSdk 2.0.1 — it became
  opt-in there (2.0.0 defaulted it on) — so the repository sets `PurviewRuntimeAsync=true` in
  `src/Directory.Build.props`, guarded by `BuildSdkAdoptionTests`. Neither switch applies to the
  `net10.0` leg, and do not expect a `net10.0` target to compile C# 15 union/closed declarations: the
  runtime types those features require are not in the .NET 10 framework.
- The two Roslyn components (`SourceGeneration`, `SourceGeneration.CodeFixes`) target `netstandard2.0`
  by SDK rule — a generator runs inside the compiler and IDE host, not on the consumer's framework.
- Uses the **Purview DotNet Project SDK** (`Purview.BuildSdk`) for project defaults and conventions.
- Uses centrally-managed package versions (`Directory.Packages.props`).
- Uses **TUnit** and **TUnit.Mocks** for testing.
- Uses `Microsoft.Testing.Platform` via package-managed tooling.
- **No build needs the Aspire CLI installed.** The example AppHosts set
  `AspireUseCliBundle=false`, so they build against the NuGet-delivered DCP and dashboard packages
  (`Aspire.Dashboard.Sdk.<rid>` / `Aspire.Hosting.Orchestration.<rid>`, added by the AppHost SDK for the
  build machine's RID). Do not put `AspireUseCliBundle=true` back in a project file: it makes a *build*
  require a resolvable CLI bundle, and resolving one is a network install performed mid-build once per
  AppHost — two of which race on the NuGet cache and fail each other with ASPIRE009. That is what broke
  the release pipeline. `ExampleAppHostTests` guards the invariant; pass `-p:AspireUseCliBundle=true` for
  a one-off build that wants the bundle path.

The skills this repository **authors** live in the root `skills/` folder — read them there, at
`skills/<name>/SKILL.md`. Skills a *package* ships are installed into `.agents/skills/` by the skills
CLI (which is gitignored here, so a fresh clone has none until you run `npx skills add`).

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
- Both PR validation and the release run the **full** suite on both target frameworks. Do not add a
  `Build:TestFilter` category filter or a workflow `test-filter` to skip the integration tests;
  `Repository.UnitTests/PipelineTests.cs` fails if the filter is narrowed.

### Test structure and style

All tests must use AAA with explicit comments:

- `// Arrange`
- `// Act`
- `// Assert`

### Naming conventions

Use naming that mirrors production structure:

- Test namespace should match the target namespace shape (same hierarchy intent).
- Test class naming: `{ClassUnderTest}Tests`
- A suite whose subject is a contract spanning several types is named for its scope instead — for
  example `ResourceKitLifecycleTests` covers the `Build`/`Configure` contract of both `HostKitBase` and
  `ResourceKitBase`. Forcing a `{ClassUnderTest}Tests` name onto one of those is less truthful, not more.
- Test method naming: `{SubjectOrMethodUnderTest}_{Scenario}_{Expectation}`

### Cancellation token rule

If any method invoked in the test body supports a `CancellationToken`, pass a token and make it the **final argument**.

- Prefer the real `CancellationToken` even when cancellation behavior is not under test, giving long running tests the opportunity to be cancelled.
- Use explicit token sources when cancellation semantics are under test.

## Completion criteria

Before considering a change done:

1. Relevant tests are updated/added using the conventions above.
2. Test runs pass for the impacted scope. Both PR validation and the release run every test project,
   including the AppHost integration tests, so `just test-unit` alone is not evidence a change passes CI.
3. New/updated docs stay consistent with this file and the authored skills in `skills/`.
4. A change to a project reference is reflected in `.config/architecture-policy.json` — the policy is
   enforced by `src/tests/Repository.UnitTests/ArchitectureTests.cs`, and widening `mayDependOn` is a
   reviewed policy change rather than a convenient edit.
5. A change to what the package ships, or to a claim the README/diagnostics pages make about it, is
   verified with `just verify-consumers` — it packs the package and builds a consumer that installs it.

## Agent skills

Authored skills live in the repository-root `skills/` folder. **Nothing packs them into a NuGet
package.** `Purview.BuildSdk` 1.0.6 removed skill packaging outright — the skills CLI clones the
*git* repository and only discovers a root `skills/` folder, so that is the one delivery route.
`PurviewSkillsToPack`, the `IncludeRepositorySkillsInPackage` shim in `src/Directory.Build.targets`,
and the `.agents/**.*` entry in `.config/purview-build.json` went with it.

`.agents/skills/` is the CLI's install location for skills from **other** repositories, and is
gitignored. This repository's own skills are never installed into it — that would leave two copies to
drift apart. Read them at their authored path, `skills/<name>/SKILL.md`.

The installed set is maintained with `just skills-update` and checked with `just skills-check`
(`Justfile.skills.just`); both need a restore first, and neither runs as part of a build.

```bash
npx skills add purview-dev/aspire-resourcekit
```

| Skill | Answers |
| --- | --- |
| `skills/purview-resourcekit-authoring/` | declaring a `[HostKit]` and resource kits, attribute styles, parent/child hierarchies, the Build/Configure lifecycle, project resources, groups and URLs |
| `skills/purview-resourcekit-options/` | generated options including nested child options, extending them, run modes (`Mode`), inherited `IsEnabled` versus `IsResourceEnabled`, `OptionsHelper` |
| `skills/purview-resourcekit-testing/` | TUnit.Aspire fixtures, overriding options from a test, hierarchy model tests, running the tiers |
| `skills/purview-resourcekit-diagnostics/` | what an `SG00xx` means, including hierarchy, group-placement and redundant-`ResourceBuilder` rules, and how to fix it |
| `skills/purview-resourcekit-migration/` | converting an existing inline AppHost into ResourceKit composition, including extracting child resource kits |

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

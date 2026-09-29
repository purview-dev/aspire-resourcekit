# Contributing

This page covers the repository layout and the day-to-day commands for working on
`Purview.Aspire.ResourceKit`. It is aimed at contributors, not consumers.

## Repository layout

- `src/src/ResourceKit` — runtime APIs consumed by AppHost projects (the packable project).
- `src/src/SourceGeneration` — the Roslyn incremental generator, analyzers, suppressor, and attributes.
- `src/src/SourceGeneration.CodeFixes` — the SG0020 IDE code fix provider.
- `src/src/Example.*` — sample Aspire applications (`Example.AppHost` generated pattern,
  `Example.ManualAppHost` manual pattern, `Example.Service`, `Example.ServiceDefaults`).
- `src/tests/*` — unit and integration tests for the runtime and the generator.
- `docs/wiki` — this documentation suite.

The NuGet package is produced from `src/src/ResourceKit/ResourceKit.csproj` and includes the runtime
APIs plus the analyzer/code-fix assemblies.

## Building and testing

The repository uses [Just](https://just.systems/) recipes (see `Justfile`). All recipes run from the
repository root.

```bash
just build          # build the solution (Debug)
just test           # run all tests
just test-unit      # run unit tests only (TUnit [Category=Unit] filter)
just restore        # restore dependencies
just pack           # produce NuGet packages into ./artifacts
just lint-check     # CSharpier formatting check
just lint-fix       # CSharpier format
just scrub          # remove bin/obj, clean, restore, stop build server
just version        # show the current version from package.json
```

## Testing conventions

- Tests use **TUnit** (and TUnit.Mocks); do not introduce NUnit, xUnit, or MSTest patterns.
- All tests follow AAA with explicit `// Arrange`, `// Act`, `// Assert` comments.
- Test names mirror production structure: `{ClassUnderTest}Tests` and
  `{SubjectOrMethodUnderTest}_{Scenario}_{Expectation}`.
- When a method under test accepts a `CancellationToken`, pass one and make it the final argument.
- Unit tests are tagged `[Category=Unit]` and are the only tests CI runs.

Test projects:

- `src/tests/ResourceKit.UnitTests` — runtime behavior (host app resources, `OptionsHelper`).
- `src/tests/ResourceKit.IntegrationTests` — starts the example AppHosts via TUnit.Aspire.
- `src/tests/SourceGeneration.UnitTests` — generator diagnostics/severity and attributes.
- `src/tests/SourceGeneration.IntegrationTests` — generated source content, caching, diagnostics,
  suppression, and the SG0020 code fix.

Reports are written to `TestResults/`.

## Agent skills and workflows

The repository carries agent skills and prompt workflows under `.agents/`. Consult them when working
on source generators, tests, the project SDK, or conventional commits. AGENTS.md is the canonical
agent guidance and references `.agents/` for reusable workflows.

## Releases

Releases are produced by the shared pipeline when a version bump is merged to `main`; see
[Release Flow](Release-Flow.md). When preparing a release:

1. Update `package.json` (the authoritative version) and align `global.json` and
   `Directory.Packages.props` with stable dependency versions.
2. Move new diagnostics from `src/src/SourceGeneration/AnalyzerReleases.Unshipped.md` into
   `src/src/SourceGeneration/AnalyzerReleases.Shipped.md` under the released version, leaving the
   unshipped file empty.
3. Add a [`CHANGELOG.md`](../../CHANGELOG.md) entry and update the wiki [release notes](Release-Notes.md).
4. Validate with `just lint-check` and `just pipeline-pack-validate` before merging.

## Pull requests

Pull requests target `main` and are validated by `.github/workflows/pr.yml`, which delegates to the
shared [Purview.Build](https://github.com/purview-dev/build) pipeline (restore, build, CSharpier lint,
unit tests, pack, package-content validation). See [Release Flow](Release-Flow.md).

Commits follow [Conventional Commits](https://www.conventionalcommits.org/), enforced by commitlint and
Lefthook hooks.
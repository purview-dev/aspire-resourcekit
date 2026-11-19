# Contributing

This page covers the repository layout and the day-to-day commands for working on
`Purview.Aspire.ResourceKit`. It is aimed at contributors, not consumers.

## Repository layout

- `src/src/ResourceKit` — runtime APIs consumed by AppHost projects (the packable project).
- `src/src/SourceGeneration` — the Roslyn incremental generator, analyzers, suppressor, and attributes.
- `src/src/SourceGeneration.CodeFixes` — the SG0020 IDE code fix provider.
- `src/src/Example.*` — sample Aspire applications:
  - `Example.Pre17.AppHost` / `Example.Pre17.ManualAppHost` — Aspire 13.6 (`Aspire.AppHost.Sdk`,
    `AddProject<T>`) generated and manual patterns.
  - `Example.AppHost17` / `Example.ManualAppHost17` — Aspire 17 Project V2
    (`Microsoft.NET.Sdk`, `AddDotnetProject`) generated and manual patterns.
  - `Example.Service` / `Example.ServiceDefaults` — the shared service and constants both families use.
- `src/tests/*` — unit and integration tests for the runtime and the generator.
- `docs/wiki` — this documentation suite, one topic folder per area.

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
just verify-consumers  # pack, assert the package shape, build a real consumer against it
just lint-check     # CSharpier formatting check
just lint-fix       # CSharpier format
just scrub          # delete bin/obj and artifacts, stop the build servers, restore from scratch
just version        # show the current version from package.json
just bump patch     # bump package.json's version (major, minor or patch)
```

The shared recipes live in `.build/just/` and are copied verbatim from the `purview-buildsdk-justfile`
skill; the root `Justfile` sets this repository's variables and adds its own recipes.

The two example families have different build contracts:

- The **Pre17** examples use `Aspire.AppHost.Sdk` 13.6.5 with `AspireUseCliBundle=false`, so they build
  against the NuGet-delivered DCP and dashboard packages (`Aspire.Dashboard.Sdk.<rid>` /
  `Aspire.Hosting.Orchestration.<rid>`) and need no Aspire CLI. Do not opt them into the bundle: resolving
  one is a network install performed mid-build, once per AppHost, and two of them race on the NuGet cache
  and fail with ASPIRE009.
- The **Aspire 17** examples use `Microsoft.NET.Sdk` + `Aspire.Hosting.AppHost` and `AddDotnetProject`.
  ASPIRE013 makes the Aspire CLI bundle a hard build requirement. Install the pinned CLI before building:

  ```bash
  dotnet tool install --global Aspire.Cli --version "$(cat .config/aspire-version)"
  aspire setup
  ```

  Both pipelines do this before the build. `ExampleAppHostTests` guards both contracts and that
  `.config/aspire-version` matches `Directory.Packages.props`' `Aspire17Version`.

## Testing conventions

- Tests use **TUnit** (and TUnit.Mocks); do not introduce NUnit, xUnit, or MSTest patterns.
- All tests follow AAA with explicit `// Arrange`, `// Act`, `// Assert` comments.
- Test names mirror production structure: `{ClassUnderTest}Tests` and
  `{SubjectOrMethodUnderTest}_{Scenario}_{Expectation}`.
- When a method under test accepts a `CancellationToken`, pass one and make it the final argument.
- The project name sets the tier: `Purview.BuildSdk` derives `Category=Unit` for `*.UnitTests`. Both
  PR validation and the release run the **full** suite, so the tier is a local convenience for the
  fast loop (`just test-unit`), not a CI gate. A test added to an integration project runs in CI.
  Do not narrow `Build:TestFilter` or pass `test-filter` from a workflow; `PipelineTests` fails if you do.

Test projects:

- `src/tests/ResourceKit.UnitTests` — runtime behavior (host app resources, `OptionsHelper`, run modes).
- `src/tests/ResourceKit.IntegrationTests` — starts the example AppHosts via TUnit.Aspire.
- `src/tests/SourceGeneration.UnitTests` — generator diagnostics/severity and attributes.
- `src/tests/SourceGeneration.IntegrationTests` — generated source content, caching, diagnostics,
  suppression, and the SG0020 code fix.
- `src/tests/Repository.UnitTests` — asserts things about this repository itself: the conformance of the
  authored skills in `skills/`, the architecture policy in `.config/architecture-policy.json`, the
  `Purview.BuildSdk` feature opt-ins (`BuildSdkAdoptionTests`), and the example AppHosts' build contract
  (no machine-global Aspire CLI).
- `src/tests/SharedTestingInfra` — a shared-testing library, referenced rather than executed.

Reports are written to `TestResults/`.

## Documentation

`docs/wiki` holds plain Markdown and nothing else — no `mkdocs.yml`, no theme or plugin list. The site
at <https://purview.dev/docs/aspire-resourcekit/> renders these files; adding site configuration here
would be ignored and drift, so don't "restore" the mkdocs scaffolding (see `CHANGELOG.md`).

The tree is hierarchical because a folder is a URL segment and a sidebar section:
`concepts/Hierarchies.md` publishes at `/docs/aspire-resourcekit/concepts/hierarchies/`.

| Path | Holds |
| --- | --- |
| `docs/wiki/*.md` | the landing page and the two pages the catalogue deep-links from a use case |
| `docs/wiki/concepts/` | how the model works: hierarchies, lifecycle, enablement, groups, project resources |
| `docs/wiki/reference/` | what the generator emits and what every attribute, helper and diagnostic means |
| `docs/wiki/testing/` | testing a resource kit |
| `docs/wiki/project/` | working on this repository: contributing, releases, versioning, toolchain, skills |
| `docs/wiki/_Sidebar.md` | the reading order the site's navigation follows |
| `docs/wiki/_redirects.yml` | every URL a moved page used to have |

Four rules keep it publishable:

- **Three pages stay at the root.** `Getting-Started.md` is `docs.rootPage` in
  `.config/purview-docs.yml`, so it renders the project's index; `Examples.md` and
  `Configuration-and-Options.md` are `useCases[].docsPage` deep links in that same file, and a
  `docsPage` is a single URL segment. Moving either means moving its catalogue reference in the same
  change.
- **`_Sidebar.md` lists every page once**, grouped in reading order. A page missing from it has no
  place in the navigation.
- **A moved page adds a line to `_redirects.yml` in the same commit.** Never delete one: the old URL
  is in a bookmark somewhere.
- **A new page is linked from another page**, and anything a consumer is told about the package is a
  claim `just verify-consumers` or the package's own tests should be able to falsify.

## Agent skills and workflows

Skills this repository **authors** live in `skills/<name>/SKILL.md`, are named with the
`purview-resourcekit` prefix, and are validated on every test run by
`src/tests/Repository.UnitTests/SkillTests.cs`. Add or change one there, not in an agent directory.

`.agents/skills/` and `.claude/skills/` are the skills CLI's install locations for skills from **other**
repositories. They are gitignored, and this repository's own skills are never installed into them — two
copies would drift. `skills-lock.json` is committed: it pins what this repository resolved.

```bash
npx skills add purview-dev/aspire-resourcekit
```

Maintain the installed set through the skills recipes, which run the skills CLI from the repository
root and need no restore:

```bash
just skills-install   # restore exactly what skills-lock.json records - how a fresh clone starts
just skills-update    # re-add every source in skills-lock.json at its latest, then fix
just skills-check     # fail if an agent directory other than .agents/skills and .claude/skills exists
```

`Skills.Agents` in `.config/purview-build-sdk.json` names the one agent thus allowed (`universal`,
the `.agents/skills` store); `just skills-fix` removes a directory it does not select. These are run
on purpose, never from a build, a git hook or a CI job that commits.

## Architecture

The dependency direction between the three layers is a policy, not a diagram:
`.config/architecture-policy.json` declares each project's responsibility and what it may reference, and
`src/tests/Repository.UnitTests/ArchitectureTests.cs` turns that into a failing build. The rule worth
knowing before you touch a project file: `ResourceKit` reaches the Roslyn components through
`OutputItemType="Analyzer"` / `ReferenceOutputAssembly="false"`, and no component may reference the
runtime — a consumer's compiler loads these assemblies, so dragging `Aspire.Hosting` into one would
break every consumer's build.

Widen `mayDependOn` only as a reviewed policy change; that is the whole point of the file.

## Releases

Releases are produced by the shared pipeline when a version bump is merged to `main`; see
[Release Flow](Release-Flow.md). When preparing a release:

1. Update `package.json` (the authoritative version) and align `global.json` and
   `Directory.Packages.props` with stable dependency versions.
2. Move new diagnostics from `src/src/SourceGeneration/AnalyzerReleases.Unshipped.md` into
   `src/src/SourceGeneration/AnalyzerReleases.Shipped.md` under the released version, leaving the
   unshipped file empty.
3. Add a [`CHANGELOG.md`](../../../CHANGELOG.md) entry and update the [release notes](Release-Notes.md).
4. Validate with `just lint-check` and `just pipeline-pack-validate` before merging.

## Pull requests

Pull requests target `main` and are validated by `.github/workflows/pr.yml`, which delegates to the
shared [Purview.Build](https://github.com/purview-dev/build) pipeline (restore, build, CSharpier lint,
the full test suite including the AppHost integration tests, pack, package-content validation). The
integration tests need a Docker-capable runner; GitHub's `ubuntu-latest` is one. See
[Release Flow](Release-Flow.md).

Commits follow [Conventional Commits](https://www.conventionalcommits.org/), enforced by commitlint and
Lefthook hooks.
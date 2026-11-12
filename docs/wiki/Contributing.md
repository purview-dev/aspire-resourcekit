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
just verify-consumers  # pack, assert the package shape, build a real consumer against it
just lint-check     # CSharpier formatting check
just lint-fix       # CSharpier format
just scrub          # remove bin/obj, clean, restore, stop build server
just version        # show the current version from package.json
```

Nothing here needs the Aspire CLI installed. The example AppHosts build against the NuGet-delivered
DCP and dashboard packages (`AspireUseCliBundle=false`), which the AppHost SDK turns into
`Aspire.Dashboard.Sdk.<rid>` and `Aspire.Hosting.Orchestration.<rid>` references for the build machine's
RID. Opting them into the CLI bundle instead (`-p:AspireUseCliBundle=true`) makes a *build* need a
resolvable bundle, and resolving one is a network install performed mid-build, once per AppHost — two of
which then race on the NuGet cache and fail each other with ASPIRE009. `ExampleAppHostTests` keeps the
opt-in out of the project files for exactly that reason.

## Testing conventions

- Tests use **TUnit** (and TUnit.Mocks); do not introduce NUnit, xUnit, or MSTest patterns.
- All tests follow AAA with explicit `// Arrange`, `// Act`, `// Assert` comments.
- Test names mirror production structure: `{ClassUnderTest}Tests` and
  `{SubjectOrMethodUnderTest}_{Scenario}_{Expectation}`.
- When a method under test accepts a `CancellationToken`, pass one and make it the final argument.
- A unit test runs in CI because of the **project it lives in**, not an attribute: `Purview.BuildSdk`
  derives the tier from the project name (`*.UnitTests` → `Category=Unit`), and the pipeline filters on
  that. Splitting a suite into a second project is how a test leaves the fast tier.

Test projects:

- `src/tests/ResourceKit.UnitTests` — runtime behavior (host app resources, `OptionsHelper`, run modes).
- `src/tests/ResourceKit.IntegrationTests` — starts the example AppHosts via TUnit.Aspire.
- `src/tests/SourceGeneration.UnitTests` — generator diagnostics/severity and attributes.
- `src/tests/SourceGeneration.IntegrationTests` — generated source content, caching, diagnostics,
  suppression, and the SG0020 code fix.
- `src/tests/Repository.UnitTests` — asserts things about this repository itself: the conformance of the
  authored skills in `skills/`, the architecture policy in `.config/architecture-policy.json`, and the
  example AppHosts' build contract (no machine-global Aspire CLI).
- `src/tests/SharedTestingInfra` — a shared-testing library, referenced rather than executed.

Reports are written to `TestResults/`.

## Documentation

`docs/wiki` holds the plain-Markdown topic pages and nothing else. There is deliberately no `Home.md`
hub, no `_Sidebar.md` navigation and no site configuration in this repository: the site at
<https://purview.dev/docs/aspire-resourcekit/> generates its own navigation from the pages that are
here. Do not "restore" the mkdocs scaffolding — it was removed on purpose (see `CHANGELOG.md`), and a
page's discoverability is decided by the site, not by a file in this repository.

Two rules that still apply: a page added or renamed here must be linked from at least one other page
or from `README.md`, and anything a consumer is told about the package is a claim
`just verify-consumers` or the package's own tests should be able to falsify.

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

Maintain the installed set through the BuildSdk targets, which run once for the whole solution:

```bash
just restore          # the targets read the restore graph and never restore on their own
just skills-update    # re-add every source in skills-lock.json
just skills-check     # fail if an agent directory other than .agents/skills and .claude/skills exists
```

`PurviewSkillsAgents` in `src/Directory.Build.props` names the two agents thus allowed; `just skills-fix`
removes a directory it does not select. These are run on purpose, never from a build, a git hook or a
CI job that commits.

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
3. Add a [`CHANGELOG.md`](../../CHANGELOG.md) entry and update the [release notes](Release-Notes.md).
4. Validate with `just lint-check` and `just pipeline-pack-validate` before merging.

## Pull requests

Pull requests target `main` and are validated by `.github/workflows/pr.yml`, which delegates to the
shared [Purview.Build](https://github.com/purview-dev/build) pipeline (restore, build, CSharpier lint,
unit tests, pack, package-content validation). See [Release Flow](Release-Flow.md).

Commits follow [Conventional Commits](https://www.conventionalcommits.org/), enforced by commitlint and
Lefthook hooks.
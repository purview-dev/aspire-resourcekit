# Changelog

All notable changes to `Purview.Aspire.ResourceKit` are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project
adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **Run modes** — `ResourceKitMode` (`All`, `LocalDevelopment`, `Run`, `Publish`) selects which AppHost
  execution a host kit or resource kit participates in. Declare the default on `[HostKit]` /
  `[ResourceDefinition]` (named property or constructor argument), override it from configuration
  (`<HostKit>:Mode`, `<HostKit>:<Resource>:Mode`), and `Build` gates on it before `IsEnabled` and
  `IsResourceEnabled(builder)`, so publish-only and local-development-only resources need no code.
- **Resource groups.** `[ResourceGroup]` marks a partial class as a named group, which can nest under
  another (`[ResourceGroup<TParent>]` or `Parent = typeof(TParent)`), and `[InGroup<TGroup>]` adds a resource kit to one or more groups.
  A group's `IsEnabled` and `Mode` (`<HostKit>:Groups:<Group>`) gate every kit in it and in its child
  groups. In run mode each group is a `ResourceKitGroupResource` dashboard node that the kit's primary
  group parents it to, and the node is excluded from the manifest. The host kit gains a typed `Groups`
  accessor. Manual host kits use `ResourceKitGroup<THostKit>` and `AddResource(kit, groups)`. A resource that is not a kit,
  such as DbGate beside SQL Server, joins a group's node with `builder.WithResourceGroup(group)`.
- **Named dashboard URLs.** `[ResourceUrl(name, url)]` adds a URL, absolute or relative to an endpoint, and
  `[EndpointUrl(endpoint)]` renames an endpoint's own URL. Each becomes a bindable `ResourceKitUrl` option
  (`<HostKit>:<Kit>:Urls:<Name>`). Unlike Aspire's relative `WithUrl("/path")`, which rewrites every
  endpoint URL, a relative `ResourceKitUrl` adds one URL for one endpoint. Manual kits override
  `ResourceKitBase.Urls`, and any builder can call `WithResourceKitUrls(...)`.
- **Diagnostics SG0021–SG0031** for groups and URLs, including cycle detection. No group can become its own
  ancestor or member, at declaration, generation or runtime.

### Changed

- **Breaking: .NET 10 is now the minimum.** The runtime targets `net10.0` only — the `net8.0` and
  `net9.0` legs are removed, so a consuming AppHost must be on .NET 10. This also drops the
  `Microsoft.Extensions.*` 10.x floor those legs previously forced onto `net8.0` consumers, which was
  the whole point: a package should not vendor a framework's abstractions to consumers on an older
  framework. Raising a framework floor requires a **major** version.
- **Repository guardrails** — `.config/architecture-policy.json` plus
  `src/tests/Repository.UnitTests/ArchitectureTests.cs` now enforce the dependency direction between the
  runtime, the Roslyn components and the samples, and `just verify-consumers` packs the package and builds
  a real consumer against it (one that installs it by `PackageReference` and is generated into, which no
  test in this repository previously proved).
- **Agent skill delivery documented correctly** — `README.md` no longer claims the package installs skills
  into a consuming repository on build. That mirror was removed in `Purview.BuildSdk` 1.0.6; the skills CLI
  is the only installer.

### Removed

- **mkdocs site configuration** — `mkdocs.yml` and the mkdocs-only navigation pages (`index.md`,
  `Home.md`, `_Sidebar.md`) are removed. The `docs/wiki` topic pages remain the source consumed by
  <https://purview.dev/docs/aspire-resourcekit/>.

### Fixed

- **The repository builds without the Aspire CLI installed.** `Example.AppHost` and
  `Example.ManualAppHost` set `AspireUseCliBundle=true`, which makes a *build* require a resolvable CLI
  bundle: with none present, `Aspire.Hosting.AppHost.targets` runs `dnx aspire.cli@13.6.4 setup` once per
  AppHost, so one solution build restores and extracts the same CLI twice, and the loser dies on the
  locked NuGet cache file with exit code 1. MSBuild reports that only as ASPIRE009 ("Automatic Aspire CLI
  bundle setup failed with exit code 1"), which failed the release pipeline on sources the PR pipeline had
  just built — a race, so it passed some runs and failed others. Both examples now build against the
  NuGet-delivered DCP and dashboard packages (`AspireUseCliBundle=false`, ASPIRE010 suppressed as
  deliberate), which is unchanged for running them; `ExampleAppHostTests` keeps the opt-in out of the
  project files.

## [1.0.0] - 2026-09-29

The first stable release of `Purview.Aspire.ResourceKit`.

### Added

- **Host kits** — `[HostKit]` marks the single host kit per compilation and generates the host base
  class, per-resource properties, typed host and resource options, and an AppHost extension method
  (`Add<HostKit>()`, for example `AddAspireResourceKit()`).
- **Resource kits** — `[ResourceDefinition]` and `[ResourceDefinition<TResource>]` mark resource kit
  classes; the generator emits the `sealed partial` skeleton, the `ResourceKitBase<TResource>` base,
  and the constructor/options wiring.
- **Predictable lifecycle** — `Build`/`BuildResource(...)` constructs each resource and
  `Configure`/`ConfigureResource()` wires resources together afterwards.
- **Enablement** — `IsEnabled` (persisted toggle, usually from generated options) and
  `IsResourceEnabled(builder)` (runtime hook) decide participation before resource construction.
- **Typed options** — generated `sealed partial` host and resource option types that bind from
  configuration, can be extended with partial classes, and are registered with `ValidateOnStart()`.
- **`OptionsHelper`** — strongly typed assignment expressions plus binder-style flattening
  (`Assign`, `AsEnvironmentVariables`, `Environment`, `Override`, `Ignore`, `PathFor`,
  `SectionNameFor`) for tests and scenario toggles.
- **Project resources** — first-class support for `AddProject<T>()`, including Aspire-generated
  `Projects.*` metadata types, with dedicated generator and analyzer validation.
- **Diagnostics SG0001–SG0020** — model validation reported by the bundled analyzer, an IDE code fix
  ("Split into separate assignments") for SG0020, and automatic `CS8618` suppression for
  non-nullable `IResourceBuilder<T>` properties.
- **Bundled agent skill** — `aspire-apphost-to-resourcekit`, auto-installed into consuming
  repositories under `.agents/skills/**` on build (opt out with
  `<EnableAgentFolderInPackage>false</EnableAgentFolderInPackage>`).

### Changed

- Host wiring is generated from attributes by default; the manual `HostKitBase<THostKit>` /
  `ResourceKitBase<THostKit, TResource>` composition pattern remains fully supported for teams that
  want explicit control.

### Diagnostics

| ID | Severity | Description |
| --- | --- | --- |
| SG0001 | Error | Class must be `partial` |
| SG0002 | Info | No resources defined for the host kit |
| SG0003 | Warning | No host kit defined (but resources exist) |
| SG0004 | Error | Multiple `[HostKit]` classes defined |
| SG0005 | Error | Duplicate resource property name |
| SG0006 | Error | Resource must derive from the expected generated base |
| SG0007 | Error | Resource name could not be derived and no `Name` was specified |
| SG0008 | Error | Explicit `PropertyName` is not a valid C# identifier |
| SG0009 | Error | Missing `IServiceCollection` type dependency |
| SG0010 | Error | Missing configuration binder dependency |
| SG0011 | Error | Missing options configuration extensions dependency |
| SG0012 | Error | Non-empty constructors are not supported on attributed classes |
| SG0013 | Error | Mixed `ResourceDefinition` attribute usage on one class |
| SG0014 | Error | Non-generic `ResourceDefinition` requires an explicit compatible base |
| SG0015 | Error | Generic `ResourceDefinition<TResource>` must not declare an explicit base |
| SG0016 | Error | No Aspire resource type could be inferred/found |
| SG0017 | Warning | `IResourceBuilder<T>` property is never assigned (execution-only) |
| SG0018 | Warning | Project resource kit does not add the declared project via `AddProject<T>()` (execution-only) |
| SG0019 | Warning | Project resource kit explicit base does not use `ProjectResource` (execution-only) |
| SG0020 | Error | `OptionsHelper.Assign` action sets more than one property path |

SG0001–SG0016 block generation; SG0017–SG0019 are execution-only warnings that never block
generation, so a resource kit with incomplete wiring is still generated.

[1.0.0]: https://github.com/purview-dev/aspire-resourcekit/releases/tag/v1.0.0

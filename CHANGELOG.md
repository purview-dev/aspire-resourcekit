# Changelog

All notable changes to `Purview.Aspire.ResourceKit` are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project
adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

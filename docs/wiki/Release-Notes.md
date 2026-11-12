# Release notes

Stable release history for `Purview.Aspire.ResourceKit`, with the highlights that matter when you
upgrade from the `1.0.0-prerelease.*` line. The full, machine-readable history lives in
[`CHANGELOG.md`](https://github.com/purview-dev/aspire-resourcekit/blob/main/CHANGELOG.md) and on
the [GitHub releases page](https://github.com/purview-dev/aspire-resourcekit/releases).

## Next (unreleased)

- **Breaking: .NET 10 is the minimum.** The `net8.0` and `net9.0` targets are gone; a consuming AppHost
  must be on `net10.0` or `net11.0`, and the package ships `lib/net10.0` (the floor) and
  `lib/net11.0` (the newest leg). See
  [Supported target frameworks](Versioning.md#supported-target-frameworks) and
  [.NET 11 toolchain and feature adoption](Toolchain.md).
- **.NET 11 leg and tooling** — the repository builds with the .NET 11 GA SDK, `Purview.BuildSdk` 2.0.1
  and TUnit 1.83. The net11.0 leg opts into the SDK's Runtime Async (`PurviewRuntimeAsync=true`) and
  takes its memory-safety rules; the net10.0 leg keeps the 10.0.15 `Microsoft.Extensions.*` floor.
  `OptionsHelper.PathFor<TOptions, TValue>` adds a non-boxing selector overload, and pack validation
  now compares against the published 1.1.0 baseline.
- **Resource groups.** `[ResourceGroup]` and `[InGroup<TGroup>]` gather kits under named, nestable groups.
  A group can be switched off or given a mode from configuration, and in run mode it appears as a node in the dashboard
  with its kits nested underneath. See [Groups and URLs](Groups-and-URLs.md).
- **Named dashboard URLs.** `[ResourceUrl]` and `[EndpointUrl]` declare a kit's or group's dashboard links as
  options. A relative URL adds one link for one endpoint, instead of rewriting every endpoint URL as
  `WithUrl("/path")` does.
- **Diagnostics SG0021-SG0031** for groups and URLs. A group can never become its own ancestor or member.

## 1.0.0

The first stable release. The public surface that shipped during the prerelease line is now frozen
under [semantic versioning](Versioning.md).

### Highlights

- **Attributes drive generation** — `[HostKit]` and `[ResourceDefinition]` /
  `[ResourceDefinition<TResource>]` produce the host kit, resource kit skeletons, typed options, and
  the AppHost extension method. See [Attributes Reference](Attributes-Reference.md) and
  [Generated Output](Generated-Output.md).
- **Deterministic lifecycle** — `Build`/`BuildResource(...)` constructs resources, then
  `Configure`/`ConfigureResource()` wires them together. See
  [Lifecycle: Build vs Configure](Lifecycle-Build-Configure.md).
- **Declarative enablement** — `IsEnabled` and `IsResourceEnabled(builder)`. See
  [Enablement](Enablement.md).
- **Typed, extendable options** — generated `sealed partial` options bound from configuration. See
  [Configuration and Options](Configuration-and-Options.md).
- **Test-friendly overrides** — `OptionsHelper` for args, environment variables, and flattening. See
  [OptionsHelper](OptionsHelper.md) and [Testing with ResourceKit](Testing-with-ResourceKit.md).
- **Project resources** — first-class `AddProject<T>()` support with SG0018/SG0019 validation. See
  [Project Resources](Project-Resources.md).
- **Diagnostics SG0001–SG0020** with an IDE code fix for SG0020. See [Diagnostics](Diagnostics.md).
- **Agent skills** — `1.0.0` bundled `aspire-apphost-to-resourcekit` and copied it into
  `.agents/skills/**` on build. That delivery is withdrawn: skills now install through the skills CLI,
  and the authored set has changed. See [Agent Skills](Agent-Skills.md).

### Upgrade notes

- No public API changes are required when moving from `1.0.0-prerelease.*` to `1.0.0`.
- Pin `Purview.Aspire.ResourceKit` to `1.0.0` (or a later compatible `1.x`).

## See also

- [Versioning and compatibility](Versioning.md)
- [Release flow](Release-Flow.md)
- [Getting Started](Getting-Started.md)

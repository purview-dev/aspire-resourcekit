# Release notes

Stable release history for `Purview.Aspire.ResourceKit`, with the highlights that matter when you
upgrade from the `1.0.0-prerelease.*` line. The full, machine-readable history lives in
[`CHANGELOG.md`](https://github.com/purview-dev/aspire-resourcekit/blob/main/CHANGELOG.md) and on
the [GitHub releases page](https://github.com/purview-dev/aspire-resourcekit/releases).

## Next (unreleased)

- **Breaking: .NET 10 is the minimum.** The `net8.0` and `net9.0` targets are gone; a consuming AppHost
  must be on `net10.0`, and the package ships a single `lib/net10.0` asset. See
  [Supported target frameworks](Versioning.md#supported-target-frameworks).

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

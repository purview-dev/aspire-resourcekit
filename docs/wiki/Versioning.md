# Versioning and compatibility

`Purview.Aspire.ResourceKit` follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
(`MAJOR.MINOR.PATCH`). The release version is authoritative in `package.json`; the release pipeline
tags the commit `v<version>` and publishes matching NuGet packages.

## What the version numbers mean

- **MAJOR** — a breaking change to the public surface (see below).
- **MINOR** — additive, backwards-compatible functionality.
- **PATCH** — backwards-compatible fixes and documentation updates.

## Prereleases

Prereleases use a hyphenated suffix (for example `1.0.0-prerelease.41`) and ship through the same
push-to-`main` pipeline. They are published so consumers can validate upcoming changes early; they
carry no stability guarantee. `1.0.0` is the first stable release.

## What counts as a breaking change

Once a version is stable, the following are treated as public contracts:

- The attributes `[HostKit]`, `[ResourceDefinition]`, and `[ResourceDefinition<TResource>]`,
  including their named arguments (`Name`, `PropertyName`, `ExtensionMethodName`, `GenerateOptions`).
- The runtime types and members consumed by AppHost code (`HostKitBase`, `ResourceKitBase`,
  `OptionsHelper`, and the `IResourceBuilder`/`WithEnvironment` extensions).
- The names and shapes of generated members (`BuildResource`, `ConfigureResource`,
  `IsResourceEnabled`, generated host properties, generated options types, and the generated
  `Add<HostKit>()` extension method).
- The diagnostic IDs `SG0001`–`SG0020`, their meanings, and their severities.

Renaming or removing any of these requires a MAJOR version and a documented migration path.

### Not covered

- The exact text of generated code and diagnostic messages.
- Files generated into a consuming repository (for example the copied `.agents/skills/**` folder).
- Build-time-only dependencies consumed through `PrivateAssets` (they never flow to consumers).

## Diagnostics as contracts

Diagnostic IDs are stable identifiers. New rules are introduced in the *unshipped* analyzer release
tracking file and only become part of a released version when they move into the *shipped* file as
part of a release. See [Contributing](Contributing.md) and [Release flow](Release-Flow.md) for the
release-time procedure.

## See also

- [Release notes](Release-Notes.md)
- [Release flow](Release-Flow.md)

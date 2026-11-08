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

## Supported target frameworks

The runtime targets **`.NET 10`** and nothing lower, so a consumer needs a `net10.0` AppHost. The
package ships a single `lib/net10.0` asset; there is no `net8.0` or `net9.0` leg to fall back to.

Raising the floor is a MAJOR version, because a consumer on an older framework cannot take the
package at all. The reverse — adding a framework — is additive as long as each leg declares its own
lowest-compatible `Microsoft.Extensions.*` floor, since the floor a published package declares becomes
the floor every consumer inherits.

The Roslyn components (`SourceGeneration`, `SourceGeneration.CodeFixes`) target `netstandard2.0`
regardless: a generator runs inside the compiler and IDE host, where the consumer's target framework
is irrelevant.

## What counts as a breaking change

Once a version is stable, the following are treated as public contracts:

- The attributes `[HostKit]`, `[ResourceDefinition]`, and `[ResourceDefinition<TResource>]`,
  including their named arguments (`Name`, `PropertyName`, `ExtensionMethodName`, `GenerateOptions`,
  `Mode`).
- The runtime types and members consumed by AppHost code (`HostKitBase`, `ResourceKitBase`,
  `ResourceKitMode`, `OptionsHelper`, and the `IResourceBuilder`/`WithEnvironment` extensions).
- The names and shapes of generated members (`BuildResource`, `ConfigureResource`,
  `IsResourceEnabled`, `Mode`, generated host properties, generated options types, and the generated
  `Add<HostKit>()` extension method).
- The diagnostic IDs `SG0001`–`SG0020`, their meanings, and their severities.

Renaming or removing any of these requires a MAJOR version and a documented migration path.

### Not covered

- The exact text of generated code and diagnostic messages.
- Build-time-only dependencies consumed through `PrivateAssets` (they never flow to consumers).

## Diagnostics as contracts

Diagnostic IDs are stable identifiers. New rules are introduced in the *unshipped* analyzer release
tracking file and only become part of a released version when they move into the *shipped* file as
part of a release. See [Contributing](Contributing.md) and [Release flow](Release-Flow.md) for the
release-time procedure.

## See also

- [Release notes](Release-Notes.md)
- [Release flow](Release-Flow.md)

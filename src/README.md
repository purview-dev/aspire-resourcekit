# Aspire Resource Isolation Package (isolated workspace)

This folder is a fully isolated package workspace under:

- `sampels/package`

It provides a reusable NuGet package that models Aspire resource composition similarly to your `AppModels`, with explicit support for:

- configuration-driven behavior,
- local mode,
- running mode,
- publishing mode,
- resource-level enable/disable and naming isolation.

## Projects

- `src/Howden.MAADevEng.Aspire.ResourceIsolation` - NuGet package source.
- `tests/Howden.MAADevEng.Aspire.ResourceIsolation.UnitTests` - unit tests (`TUnit`, `TUnit.Mocks`).
- `tests/Howden.MAADevEng.Aspire.ResourceIsolation.IntegrationTests` - Aspire integration tests (`TUnit.Aspire`).
- `examples/ResourceIsolation.Example.AppHost` - local/running example AppHost.
- `examples/ResourceIsolation.Example.PublishAppHost` - publish-aware example AppHost.
- `examples/ResourceIsolation.Example.Service` - minimal service for examples/tests.

## Core usage

1. Load settings from configuration:
   - `ConfigurationIsolationSettingsProvider`
2. Build an `AppIsolationContext`:
   - mode is resolved from config and publish runtime.
3. Register resources with `IsolatedResourceCollection`:
   - use `DelegateIsolatedResource<TResource>` for full flexibility.
4. Call `Initialise(builder)`:
   - builds first, then configures dependencies.

## Packing

Run packaging from this folder against:

- `src/Howden.MAADevEng.Aspire.ResourceIsolation/Howden.MAADevEng.Aspire.ResourceIsolation.csproj`

The package metadata is already configured (`PackageId`, symbols, readme, version).

## Notes

- This workspace intentionally does not depend on your `src/backend` projects.
- Resource names can be isolated with prefix/suffix and overrides per resource key.

# Aspire Resource Isolation Package (isolated workspace)

It provides a NuGet package for Aspire resource composition:

- configuration-driven behaviour,
- local mode,
- running mode,
- publishing mode,
- resource-level enable/disable and naming isolation.

## Projects

- `src/ResourceKit` - NuGet package source.
- `tests/ResourceKit.UnitTests` - unit tests (`TUnit`, `TUnit.Mocks`).
- `tests/ResourceKit.IntegrationTests` - Aspire integration tests (`TUnit.Aspire`).
- `src/Example.AppHost` - local/running example AppHost.
- `src/Example.PublishAppHost` - publish-aware example AppHost.
- `src/Example.Service` - minimal service for examples/tests.

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

- `src/ResourceKit/ResourceKit.csproj`

or

- `just pack`

The package metadata is already configured (`PackageId`, symbols, readme, version).

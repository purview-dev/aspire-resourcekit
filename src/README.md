# ResourceKit solution workspace

This folder contains the .NET solution for `Purview.Aspire.ResourceKit`, including runtime library code, source generation, tests, and example apps.

## Layout

### Source projects

- `src/ResourceKit` — runtime APIs consumed by AppHost projects.
- `src/SourceGeneration` — Roslyn incremental generator (attributes + generated host/resource wiring).
- `src/Example.AppHost` — sample AppHost using generated ResourceKit composition.
- `src/Example.ManualAppHost` — sample AppHost showing manual composition patterns.
- `src/Example.Service` — sample service used by examples/tests.
- `src/Example.ServiceDefaults` — shared service defaults for examples.

### Test projects

- `tests/ResourceKit.UnitTests`
- `tests/ResourceKit.IntegrationTests`
- `tests/SourceGeneration.UnitTests`
- `tests/SourceGeneration.IntegrationTests`

Reports are written to `TestResults/`.

## Day-to-day workflow

1. Update runtime/source-generation code under `src/src`.
2. Validate behavior through the corresponding `src/tests` project(s).
3. Exercise the end-to-end examples in `src/src/Example.*` when making API or generation changes.

## Packaging

The NuGet package is produced from:

- `src/src/ResourceKit/ResourceKit.csproj`

The package includes runtime APIs plus the source generator analyzer assembly.

## Consumer documentation

For package usage guidance (attributes, generated output, configuration, and examples), see:

- `../README.md`
- `src/ResourceKit/README.md`

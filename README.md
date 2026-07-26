# Purview.Aspire.ResourceKit

`Purview.Aspire.ResourceKit` helps you structure Aspire AppHost resource composition as testable, discoverable classes, with source-generated glue code for host apps, resource options, and registration extensions.

## What you get

- Attribute-driven source generation for host app/resource wiring.
- Strongly typed resource composition via reusable base classes.
- Generated options support for host and per-resource configuration.
- Predictable build/configure lifecycle for cross-resource dependencies.

## Install

Add the NuGet package to your AppHost project:

- Package ID: `Purview.Aspire.ResourceKit`

## Quick start

1. Create a partial host app type and annotate it with `[HostApp]`.
2. Create partial resource types and annotate each with `[ResourceDefinition]`.
3. Register everything with `builder.Add{HostAppName}ResourceKit()`.

### Example

```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Purview.Aspire.ResourceKit;

[HostApp(Name = "Example")]
sealed partial class ExampleHostApp;

[ResourceDefinition<ProjectResource>("api")]
sealed partial class ApiResource
{
    protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder)
        => builder.AddProject<Projects.Example_Service>(Name);
}

var builder = DistributedApplication.CreateBuilder(args);
builder.AddExampleHostAppResourceKit();
```

## Generated artifacts

For each `[HostApp]`, the generator emits:

- a host-app resource base class (`{HostAppName}ResourceBase<TResource>`),
- resource properties on your host app partial class,
- host app options (`{HostAppName}Options`) when enabled,
- per-resource options types (`{ResourceType}Options`) when enabled,
- an extension method (`Add{HostAppName}ResourceKit`) to register and build the host app.

## Configuration

When options generation is enabled (default):

- host app options bind from section `{HostAppClassName}`,
- resource options bind from section `{GeneratedPropertyName}`.

You can disable resources by name using `{HostAppClassName}Options.DisabledResources`, or dynamically using `IsResourceEnabledPredicate`.

## Repository layout

- `src/src/ResourceKit` — runtime package source.
- `src/src/SourceGeneration` — Roslyn source generator.
- `src/tests/*` — unit and integration tests.
- `src/src/Example.*` — sample host/service projects.

## Additional docs

- Package usage details: `src/src/ResourceKit/README.md`
- Solution workspace notes: `src/README.md`

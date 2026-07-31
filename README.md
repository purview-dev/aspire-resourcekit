# Purview.Aspire.ResourceKit

`Purview.Aspire.ResourceKit` is a source-generator-powered framework for structuring .NET Aspire AppHost resource composition as strongly typed, test-friendly classes.

If your AppHost is getting bigger, this helps you keep resource setup maintainable and discoverable by moving composition into focused resource classes and generating the plumbing for you.

> [!TIP]
> **Lifecycle quick links**
>
> - Build vs Configure guidance: [`docs/getting-started.md#5-understand-build-vs-configure-before-adding-dependencies`](docs/getting-started.md#5-understand-build-vs-configure-before-adding-dependencies)
> - Runtime enablement (`IsEnabled` + `IsResourceEnabled(...)`): [`docs/configuration.md#isenabled-vs-isresourceenabled`](docs/configuration.md#isenabled-vs-isresourceenabled)

## Why teams use it

- **Cleaner AppHost code** with resource logic split into dedicated classes.
- **Strong typing + IntelliSense** instead of stringly-typed setup.
- **Generated wiring** for host/resource options and registration.
- **Predictable lifecycle** (`Build` then `Configure`) for inter-resource dependencies.
- **Testability** with options overrides and isolated resource composition.

## Quick example

```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Purview.Aspire.ResourceKit;

[HostKit]
partial class ShopHostKit;

[ResourceDefinition<ProjectResource>("api")]
partial class ApiResourceKit
{
    protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder)
        => builder.AddProject<Projects.Example_Service>(Name);
}

var builder = DistributedApplication.CreateBuilder(args);
builder.AddAspireResourceKit();
```

The extension method name is generated from your host metadata; in this repository sample it is `AddAspireResourceKit()`.

> [!TIP]
> **Built-in AI agent skills (auto-installed via NuGet)**
>
> This package can ship one or more bundled [Agent Skills](https://agentskills.io/).
> When a consuming project builds, any `skills/**/SKILL.md` files in the package are copied to:
>
> - `.agents/skills/**`
>
> Example:
>
> - `skills/aspire-apphost-to-resourcekit/SKILL.md` → `.agents/skills/aspire-apphost-to-resourcekit/SKILL.md`
>
> It also writes a local `.gitignore` into each generated skill folder to keep updates out of source control noise.
>
> To disable this behavior, set the shared opt-out property in your project (or `Directory.Build.props`):
>
> `<EnableEmbeddedAgentSkills>false</EnableEmbeddedAgentSkills>`

## Lifecycle mental model (early cheat sheet)

ResourceKit has two layers of lifecycle methods:

- `Build` / `BuildResource(...)`: **construct this resource** (`AddProject`, `AddRedis`, etc.).
- `Configure` / `ConfigureResource()`: **attach resources to each other** (references, bindings, cross-resource wiring).

Enablement is evaluated *before* resource construction:

- `IsEnabled` is the persisted toggle (typically from generated options).
- `IsResourceEnabled(builder)` is the runtime hook used by `Build` to decide whether this resource should run now.
- If it evaluates to `false`, both `BuildResource(...)` and `ConfigureResource()` are skipped for that resource.

Use `IsResourceEnabled(builder)` when enablement depends on runtime state (environment, config, publish mode, etc.), not just static options.

## What gets generated

From your `[HostKit]` and `[ResourceDefinition]` declarations, ResourceKit generates:

- a host resource base class,
- resource properties on the host,
- host + per-resource options (when enabled),
- an AppHost extension method to build/configure/register your host kit.

## Learn more

- NuGet package deep dive (API and generator behavior): [`src/src/ResourceKit/README.md`](src/src/ResourceKit/README.md)
- Getting started guide: [`docs/getting-started.md`](docs/getting-started.md)
- Configuration and options patterns: [`docs/configuration.md`](docs/configuration.md)
- Diagnostics and troubleshooting: [`docs/diagnostics.md`](docs/diagnostics.md)
- Workspace notes for contributors: [`src/README.md`](src/README.md)

## Repository layout

- `src/src/ResourceKit` — runtime package source.
- `src/src/SourceGeneration` — Roslyn source generator.
- `src/src/Example.*` — sample Aspire applications.
- `src/tests/*` — unit and integration tests.

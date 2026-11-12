# Aspire Resource Kit

[![NuGet version](https://img.shields.io/nuget/v/Purview.Aspire.ResourceKit.svg)](https://www.nuget.org/packages/Purview.Aspire.ResourceKit)
[![Release](https://github.com/purview-dev/aspire-resourcekit/actions/workflows/release.yml/badge.svg)](https://github.com/purview-dev/aspire-resourcekit/actions/workflows/release.yml)

`Purview.Aspire.ResourceKit` is a source-generator-powered framework for structuring .NET Aspire AppHost resource composition as strongly typed, test-friendly classes.

If your AppHost is getting bigger, this helps you keep resource setup maintainable and discoverable by moving composition into focused resource classes and generating the plumbing for you.

> [!IMPORTANT]
> Requires **.NET 10**. The package ships `net10.0` and `net11.0` assets — there is no `net8.0` or
> `net9.0` asset. A .NET 10 AppHost resolves the `net10.0` asset; a .NET 11 AppHost resolves the
> `net11.0` asset.
> See [Versioning and compatibility](docs/wiki/Versioning.md#supported-target-frameworks).
>
> For what changed, see the [changelog](CHANGELOG.md) and [release notes](docs/wiki/Release-Notes.md).

> [!TIP]
> **Lifecycle quick links**
>
> - Build vs Configure guidance: [`docs/wiki/Lifecycle-Build-Configure.md`](docs/wiki/Lifecycle-Build-Configure.md)
> - Runtime enablement and run modes (`Mode` + `IsEnabled` + `IsResourceEnabled(...)`): [`docs/wiki/Enablement.md`](docs/wiki/Enablement.md)

## Why teams use it

- **Cleaner AppHost code** with resource logic split into dedicated classes.
- **Strong typing + IntelliSense** instead of stringly-typed setup.
- **Generated wiring** for host/resource options and registration.
- **Parent/child resource hierarchies** for VNETs and subnets, SQL servers and databases, Service Bus
  namespaces and topics/queues/subscriptions, or storage accounts and containers (issue #57).
- **Predictable lifecycle** (`Build` then `Configure`) for inter-resource dependencies.
- **Hierarchical kits** so a parent resource can own child kits, with typed `Parent` access, parent-first
  build ordering, inherited enablement, and nested options.
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
> **Agent skills for your coding agent**
>
> This repository authors [Agent Skills](https://agentskills.io/) describing ResourceKit — declaring a
> `[HostKit]` and resource kits, the generated options and run modes, test fixtures, and what each
> `SG00xx` diagnostic means.
>
> They are **not** shipped in the NuGet package, and nothing copies them into your repository on build.
> The skills CLI clones the git repository and installs them into your agent's own skill directory:
>
> ```bash
> npx skills add purview-dev/aspire-resourcekit
> ```
>
> Append `#<ref>` to pin a commit or tag. The CLI records the installed set in `skills-lock.json`, so it
> can be restored later with `npx skills experimental_install`.

## Lifecycle mental model

ResourceKit has two layers of lifecycle methods:

- `Build` / `BuildResource(...)`: **construct this resource** (`AddProject`, `AddRedis`, etc.).
- `Configure` / `ConfigureResource()`: **attach resources to each other** (references, bindings, cross-resource wiring).

Enablement is evaluated *before* resource construction:

- `Mode` selects which AppHost execution a kit participates in (`All`, `LocalDevelopment`, `Run`, `Publish`). It is configured through the generated options and can be declared on `[HostKit]`/`[ResourceDefinition]`.
- `IsEnabled` is the persisted toggle (typically from generated options) and the single member consumers need to check.
- `IsResourceEnabled(builder)` is the runtime hook used by `Build` to decide whether this resource should run now.
- If any gate excludes the resource, both `BuildResource(...)` and `ConfigureResource()` are skipped for that resource.

Use `Mode` for publish-only or local-development-only resources, and `IsResourceEnabled(builder)` when enablement depends on runtime state beyond an execution mode (environment probes, dynamic configuration).

## What gets generated

From your `[HostKit]` and `[ResourceDefinition]` declarations, ResourceKit generates:

- a host resource base class,
- resource properties on the host,
- host + per-resource options (when enabled),
- typed resource groups (`[ResourceGroup]`, `[InGroup<TGroup>]`) that switch sets of resources on or off and nest them in the dashboard,
- named dashboard URLs (`[ResourceUrl]`, `[EndpointUrl]`) that options can override,
- an AppHost extension method to build/configure/register your host kit.

## Documentation

- [Homepage](https://purview.dev/projects/aspire-resourcekit/)
- [Documentation](https://purview.dev/docs/aspire-resourcekit/)
- [Getting started](docs/wiki/Getting-Started.md)
- [Configuration and options](docs/wiki/Configuration-and-Options.md)
- [Parent and child resource kits](docs/wiki/Hierarchies.md)
- [Resource groups and dashboard URLs](docs/wiki/Groups-and-URLs.md)
- [Diagnostics and troubleshooting](docs/wiki/Diagnostics.md)
- [Examples: generated vs manual](docs/wiki/Examples.md)
- [Release notes](docs/wiki/Release-Notes.md)
- [Versioning and compatibility](docs/wiki/Versioning.md)
- [.NET 11 toolchain and feature adoption](docs/wiki/Toolchain.md)
- [Changelog](CHANGELOG.md)
- NuGet package deep dive (API and generator behavior): [`src/src/ResourceKit/Sdk/README.md`](src/src/ResourceKit/Sdk/README.md)
- Workspace notes for contributors: [`src/README.md`](src/README.md)

## Repository layout

- `src/src/ResourceKit` — runtime package source.
- `src/src/SourceGeneration` — Roslyn source generator.
- `src/src/Example.*` — sample Aspire applications.
- `src/tests/*` — unit and integration tests.

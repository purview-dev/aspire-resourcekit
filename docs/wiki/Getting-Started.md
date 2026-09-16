# Getting started

This guide walks through a minimal host + resource setup using source generation. It assumes you have
an Aspire AppHost project and a service project ready to compose.

> [!TIP]
> For a quick jump across lifecycle concepts, see [Lifecycle: Build vs Configure](Lifecycle-Build-Configure.md)
> and [Enablement](Enablement.md).

## Install the package

Install the `Purview.Aspire.ResourceKit` package in your AppHost project.

```bash
dotnet add package Purview.Aspire.ResourceKit
```

> [!TIP]
> This package can include one or more bundled [Agent Skills](https://agentskills.io/).
> On build, `skills/**/SKILL.md` entries are copied to `.agents/skills/**` in the consuming repository
> (for example: `skills/aspire-apphost-to-resourcekit/SKILL.md` →
> `.agents/skills/aspire-apphost-to-resourcekit/SKILL.md`), along with a local `.gitignore` in each
> generated skill folder to keep updates out of source control noise.
>
> To opt out, set `<EnableAgentFolderInPackage>false</EnableAgentFolderInPackage>` in your project
> (or `Directory.Build.props`). See [Bundled Agent Skills](Agent-Skills.md).

## Define a host kit

Create a partial class and annotate it with `[HostKit]`.

```csharp
using Purview.Aspire.ResourceKit;

[HostKit]
partial class ShopHostKit;
```

## Define one or more resources

Create a partial resource class per resource and annotate it with `[ResourceDefinition]`.

```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Purview.Aspire.ResourceKit;

[ResourceDefinition<Projects.Example_Service>("api")]
partial class ApiResourceKit
{
    protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder)
        => builder.AddProject<Projects.Example_Service>(Name);
}
```

> [!NOTE]
> For a project resource you can use the Aspire-generated project reference type
> (`Projects.Example_Service`, the type accepted by `AddProject<T>()`) instead of the concrete
> `ProjectResource`. The generator maps it to `ProjectResource` for the generated base class, and the
> analyzer validates that `BuildResource` adds the same project via `AddProject<T>()` (SG0018) and that an
> explicit base, when used, resolves to `ProjectResource` (SG0019). See
> [Project Resources](Project-Resources.md).

### Choose an attribute style

You have two valid styles:

- **Generic style**: `[ResourceDefinition<TResource>(...)]`
  - Preferred for most cases.
  - Do not declare an explicit base type.
- **Non-generic style**: `[ResourceDefinition(...)]`
  - Requires an explicit compatible base type that provides the resource type.

Example non-generic style:

```csharp
[ResourceDefinition("api")]
partial class ApiResourceKit : ResourceKitBase<ProjectResource>
{
    protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder)
        => builder.AddProject<Projects.Example_Service>(Name);
}
```

Avoid mixing both styles on the same class. See [Attributes Reference](Attributes-Reference.md) for
the full rules.

## Register generated wiring in AppHost

Call the generated extension method from your AppHost entry point.

```csharp
var builder = DistributedApplication.CreateBuilder(args);
builder.AddAspireResourceKit();
```

The method name can be customized with `HostKitAttribute.ExtensionMethodName`.

## Extend generated typed options

ResourceKit generates typed options as nested `sealed partial` classes, so you can extend them with
your own settings. Host options are extended on the host kit; resource options are extended on each
resource kit.

```csharp
[ResourceDefinition<ProjectResource>("api")]
sealed partial class ApiResourceKit
{
    partial class ApiResourceKitOptions
    {
        public string PublishEnvironmentVariableName { get; set; } = "PUBLISH_MARKER";
    }
}
```

See [Configuration and Options](Configuration-and-Options.md) for the full pattern.

## Understand Build vs Configure

ResourceKit has two lifecycle pairs:

- `Build` / `BuildResource(...)` builds the resource itself.
- `Configure` / `ConfigureResource()` wires resources together after build.

Think of it as:

- **Build phase**: create each resource.
- **Configure phase**: connect the created resources.

See [Lifecycle: Build vs Configure](Lifecycle-Build-Configure.md) and
[Enablement](Enablement.md) before adding cross-resource dependencies.

## Expand incrementally

A common progression:

- Start with one resource kit class.
- Add more resource kit classes as the AppHost grows.
- Use generated options to toggle resources for local/dev/test scenarios.

For configuration details, continue with [Configuration and Options](Configuration-and-Options.md).
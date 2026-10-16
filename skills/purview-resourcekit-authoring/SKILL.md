---
name: purview-resourcekit-authoring
description: >-
  Use when composing an Aspire AppHost with Purview.Aspire.ResourceKit - declaring a [HostKit] and
  [ResourceDefinition] resource kits, choosing the generic or non-generic attribute style, the
  Build/BuildResource and Configure/ConfigureResource lifecycle, project resources through
  AddProject, and registering the generated AddAspireResourceKit extension.
metadata:
  product: Purview.Aspire.ResourceKit
  repository: purview-dev/aspire-resourcekit
---

# Composing an AppHost with ResourceKit

`Purview.Aspire.ResourceKit` turns an inline Aspire AppHost into a declarative model: one host kit,
one resource kit per resource, and a generated extension method that wires them together.

```bash
dotnet add package Purview.Aspire.ResourceKit
```

Both public attributes are **generated into the consuming compilation** by the bundled source
generator — there is no separate attribute package to reference.

## The shape

```csharp
using Purview.Aspire.ResourceKit;

[HostKit]
partial class ShopHostKit;
```

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

```csharp
var builder = DistributedApplication.CreateBuilder(args);
builder.AddAspireResourceKit();
```

Exactly **one** `[HostKit]` per compilation (`SG0004`); split scenarios into separate projects if you
need more. Every participating class must be `partial` (`SG0001`).

## `[HostKit]`

| Argument | Type | Default | Purpose |
| --- | --- | --- | --- |
| `Name` | `string?` | `null` | controls generated naming |
| `ExtensionMethodName` | `string?` | derived from the host kit name | renames the generated builder extension |
| `GenerateOptions` | `bool` | `true` | enables generated host options |

## `[ResourceDefinition]`

| Argument | Type | Default | Purpose |
| --- | --- | --- | --- |
| `Name` | `string?` | derived from the class name, suffix trimmed | the logical Aspire resource name |
| `PropertyName` | `string?` | derived from the class name | the generated host property name; must be a valid C# identifier (`SG0008`) |
| `AspireResourceType` | generic type argument | `null` | the Aspire `IResource` type — generic style only |

### Two styles, and you pick exactly one per class

**Generic (recommended)** — the resource type is on the attribute, and the generator supplies the
base:

```csharp
[ResourceDefinition<ProjectResource>("api")]
partial class ApiResourceKit
{
    protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder)
        => builder.AddProject<Projects.Example_Service>(Name);
}
```

Do **not** declare an explicit base (`SG0015`). The type argument may be an Aspire resource type
implementing `IResource` (used as-is) or an `IProjectMetadata` type — the type `AddProject<TProject>()`
accepts — which is mapped to `ProjectResource`. Anything else is `SG0016`.

**Non-generic** — the resource type comes from an explicit base:

```csharp
[ResourceDefinition("api")]
partial class ApiResourceKit : ResourceKitBase<ProjectResource>
{
    protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder)
        => builder.AddProject<Projects.Example_Service>(Name);
}
```

You **must** declare a valid explicit base (`SG0014`), deriving from the generated
`ResourceKitBase<TResource>` or the runtime `ResourceKitBase<THostKit, TResource>` (`SG0006`).

| Situation | Diagnostic |
| --- | --- |
| both styles on one class | `SG0013` |
| generic style with an explicit base | `SG0015` |
| non-generic style without one | `SG0014` |
| explicit base not a valid resource-kit base | `SG0006` |
| resource type cannot be inferred | `SG0016` |

### Constructors

An attributed class must not declare a parameterised constructor or an executable constructor body
(`SG0012`). The generator supplies the wiring — a resource kit's primary constructor receives the
host kit and its options.

## Build versus Configure

Two lifecycle pairs, and keeping them separate is what makes the composition deterministic:

- **`Build` / `BuildResource(...)`** — *construct* this resource: `AddProject`, `AddRedis`,
  `AddAzureStorage`.
- **`Configure` / `ConfigureResource()`** — *attach* resources to each other after construction:
  references, bindings, cross-resource wiring.

When the generated extension runs, before `DistributedApplication.Build()` completes:

1. resource kits are instantiated from options;
2. every enabled resource is **built**;
3. every enabled resource is **configured**.

So by the time `ConfigureResource()` runs, every resource exists and is reachable through the host
kit's generated properties:

```csharp
protected override void ConfigureResource()
{
    ResourceBuilder.WithReference(HostKit.Postgres.Database).WaitFor(HostKit.Postgres.Database);

    if (HostKit.Redis.IsEnabled)
        ResourceBuilder.WithReference(HostKit.Redis).WaitFor(HostKit.Redis);
}
```

`HostKitBase<THostKit>` enforces that resources cannot be added after `Build` seals the list, so the
lifecycle is single-run.

`Build` stores the result of your `BuildResource(...)` in `ResourceBuilder`. Every
`IResourceBuilder<T>` property on a resource kit — nullable or not — must be assigned in
`BuildResource` or `ConfigureResource`, or `SG0017` reports it. (`CS8618` is suppressed for these
properties automatically, since they are populated during the lifecycle, which is why `SG0017` is the
signal that matters.)

### Hooks on the extension method

```csharp
builder.AddAspireResourceKit(
    onBuilt: (hostKit, builder) => { /* after resources are created, before Configure */ },
    onConfigured: hostKit => { /* after all resources are configured */ },
    configureOptions: optionsBuilder => { /* additional options configuration */ }
);
```

## Project resources

Use the Aspire-generated project reference type directly in the attribute. When the type argument
implements `IProjectMetadata`, ResourceKit validates it, maps it to `ProjectResource` for the
generated base, and enables two execution-only rules:

- **`SG0018`** — `BuildResource` must register the **declared** project via `AddProject<T>()`.
  Building something else (`builder.AddContainer("api", "my-image")`) is a warning, because at runtime
  the declared project is never registered and the resource fails.
- **`SG0019`** — a project resource kit with an explicit base must resolve to `ProjectResource`.

Both are warnings rather than errors on purpose: generation still proceeds, so you can finish the
override instead of losing the host kit output.

## Enablement gates both phases

Before `BuildResource(...)` runs, `IsResourceEnabled(builder)` is evaluated — **only when `IsEnabled`
is already `true`** — and its result is assigned back to `IsEnabled`. A `false` result skips both
`BuildResource(...)` and `ConfigureResource()`. The `purview-resourcekit-options` skill covers the
full model, including why a kit disabled through options is never re-enabled by the hook.

## Growing a composition

Start with one resource kit, add one class per resource, and use the generated options to toggle
resources for local, dev and test scenarios. Prefer a resource-level toggle to conditional host code
— it keeps the model declarative and testable.

## Related skills

`purview-resourcekit-options` for generated options, enablement and `OptionsHelper`;
`purview-resourcekit-diagnostics` for the `SG00xx` catalogue; `purview-resourcekit-testing` for test
hosts; and `purview-resourcekit-migration` for converting an existing inline AppHost.

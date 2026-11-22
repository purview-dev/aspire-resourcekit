---
name: purview-resourcekit-authoring
description: >-
  Use when composing an Aspire AppHost with Purview.Aspire.ResourceKit - declaring a [HostKit] and
  [ResourceDefinition] resource kits, choosing the generic or non-generic attribute style, the
  Build/BuildResource and Configure/ConfigureResource lifecycle, project resources through
  AddProject or AddDotnetProject (Aspire 17 Project V2), ProjectPath/ResourcePath with a generated
  BuildResource, typed AppHostProjects paths, parent/child kits with Parent = typeof(TParentKit) and inherited enablement, grouping
  kits with [ResourceGroup]/[InGroup<TGroup>], named dashboard URLs with
  [ResourceUrl]/[EndpointUrl], and registering the generated AddAspireResourceKit extension.
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

Requires **.NET 10** or **.NET 11**: the package ships `lib/net10.0` and `lib/net11.0` assets, and the
attributes below are generated into the consuming compilation by the bundled source generator —
there is no separate attribute package to reference.

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
| `Mode` | `ResourceKitMode` | `All` | execution mode for the host and every resource it composes |

## `[ResourceDefinition]`

| Argument | Type | Default | Purpose |
| --- | --- | --- | --- |
| `Name` | `string?` | derived from the class name, suffix trimmed | the logical Aspire resource name |
| `PropertyName` | `string?` | derived from the class name | the generated host or parent property name; must be a valid C# identifier (`SG0008`) |
| `Mode` | `ResourceKitMode` | `All` | execution mode for this resource |
| `Parent` | `Type?` | `null` | the owning resource kit: `Parent = typeof(TParentKit)` |
| `ProjectPath` | `string?` | `null` | Aspire 17 Project V2 project path (`AddDotnetProject`); only used with `DotnetProjectResource` (`SG0041`) |
| `ResourcePath` | `string?` | `null` | the path for any path-based resource (`AddDotnetProject`, the JavaScript `Add*App` methods); wins over `ProjectPath` (`SG0042`), ignored on other resources (`SG0043`) |
| `AspireResourceType` | generic type argument | `null` | the Aspire `IResource` type — generic style only |

`Mode` accepts a named property or a constructor argument on both attributes, and configuration can
override the generated default:

```csharp
[ResourceDefinition<AzureKeyVaultResource>("kv", ResourceKitMode.Publish)]  // publish only
[ResourceDefinition<ProjectResource>(Mode = ResourceKitMode.LocalDevelopment)]  // local dev only
```

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
implementing `IResource` (used as-is), an `IProjectMetadata` type — the type `AddProject<TProject>()`
accepts — which is mapped to `ProjectResource`, or `DotnetProjectResource` for Aspire 17 Project V2.
Anything else is `SG0016`.

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

## Parent and child kits

`Parent = typeof(TParentKit)` turns a kit into a child. The generator emits a typed `Parent` property on the
child and a typed child property on the parent, and constructs parents before children:

```csharp
[ResourceDefinition<AzurePostgresFlexibleServerResource>("postgres")]
sealed partial class PostgresKit
{
    protected override IResourceBuilder<AzurePostgresFlexibleServerResource>
        BuildResource(IDistributedApplicationBuilder builder) =>
            builder.AddAzurePostgresFlexibleServer(Name);
}

[ResourceDefinition<AzurePostgresFlexibleServerDatabaseResource>(
    "db", PropertyName = "Database", Parent = typeof(PostgresKit))]
sealed partial class PostgresDatabaseKit
{
    protected override IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource>
        BuildResource(IDistributedApplicationBuilder builder) =>
            Parent.AddDatabase(Name);
}
```

- The child uses `Parent` directly; the parent has already finished `BuildResource`, and a resource kit is
  itself an `IResourceBuilder<TResource>` (`Parent.ResourceBuilder` is redundant; SG0040).
- `hostKit.Postgres.Database` is the strongly typed child; `Parent` is the strongly typed parent.
- One parent per kit, any number of children, any depth.
- A disabled parent disables every descendant, and a disabled ancestor never invokes a descendant's
  `IsResourceEnabled` hook. Siblings are independent.
- Child options nest under the parent (`ShopHostKit:Postgres:Database:IsEnabled`).
- A child may declare `[InGroup]`; with none it inherits its parent's **primary** group. Grouping never
  changes build order. An explicit child group whose primary group differs from the parent's warns `SG0039`;
  the child renders in its own group unless an Aspire visual parent places it under the parent.
- ResourceKit parentage is composition ownership, not a general dependency, and it does **not** create an
  Aspire visual parent relationship. Add `WithParentRelationship(Parent.ResourceBuilder)` yourself when you
  want that.
- Manual kits use `ResourceKitBase(hostKit, parent, name)` and register both kits with the host; the host
  validates the graph before creating resources.

The VNET/subnet shape from issue #57 is the same: a `VnetKit` root and one `SubnetKit` per subnet with
`Parent = typeof(VnetKit)`, each building with `Parent.AddSubnet(Name, prefix)`. Invalid
graphs report `SG0032`–`SG0038`.

## Build versus Configure

Two lifecycle pairs, and keeping them separate is what makes the composition deterministic:

- **`Build` / `BuildResource(...)`** — *construct* this resource: `AddProject` / `AddDotnetProject`, `AddRedis`,
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
    if (HostKit.Postgres.IsEnabled && HostKit.Postgres.Database.IsEnabled)
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

Declare each project or app directory once and let the package's build target emit typed paths. This
covers Project V2 `.csproj` paths, which Aspire's `Projects.*` metadata does not contain when the
service is added by path, and Vite/npm directories, which never had a typed reference:

```xml
<ItemGroup>
  <ResourceKitProject Include="..\..\dotnet-project" Name="api" />
  <ResourceKitProject Include="..\..\..\web\packages\vite-app" Name="web" />
  <ResourceKitPath Include="..\..\deploy\Dockerfile" Name="dockerfile" Kind="File" />
</ItemGroup>
```

```csharp
// No BuildResource: ResourceKit generates AddDotnetProject(builder, Name, "<the value of AppHostProjects.Api.Path>").
[ResourceDefinition<DotnetProjectResource>(AppHostProjects.Api.Name, ProjectPath = AppHostProjects.Api.Path)]
partial class ApiResourceKit;
```

- `Path` is a `const` relative to the AppHost project directory, so the generator can read it, and
  SG0018 can validate it in a hand-written override. The class is in the AppHost's root namespace and
  is `internal` unless `ResourceKitAppHostProjectsVisibility` changes it.
- `Name` defaults to the leaf file/directory name; `ClassName` overrides the nested type name. A
  missing path is `RKP0001` (set `Optional="true"` when it may be absent), an invalid class name is
  `RKP0002`, an invalid resource name is `RKP0003`, a duplicate nested class name is `RKP0004`, a
  `Kind="File"`/`"Directory"` mismatch is `RKP0005`, and an unsupported `Kind` is `RKP0006`.
- `ResourceKitProject` is for a .NET project or app directory; `ResourceKitPath` is for any other file
  or directory. Both generate `AppHostProjects` and are validated before compilation.
- The class is emitted by an MSBuild target, not the source generator, so it also works with
  `DisablePurviewAspireResourceKitSourceGenerator=true`. A project-reference consumer must import
  `Sdk/buildTransitive/Purview.Aspire.ResourceKit.targets` explicitly; a package consumer gets it
  automatically.
- This is C#-only: F#/VB AppHosts fail with `RKP0007`, and TypeScript (`apphost.mts`) / Go
  (`apphost.go`) AppHosts have no MSBuild project and use the Aspire CLI's generated SDK modules
  instead.

For a JavaScript app, declare `AppHostProjects.Web.Path` with `ResourcePath`. A Vite or Next.js kit
gets `AddViteApp`/`AddNextJsApp` generated with the default `"dev"` script. A Node, Bun or Deno kit
needs a script path, so you write that override, and SG0018 validates its path:

```csharp
[ResourceDefinition<ViteAppResource>(AppHostProjects.Web.Name, ResourcePath = AppHostProjects.Web.Path)]
partial class WebResourceKit;

[ResourceDefinition<NodeAppResource>(AppHostProjects.Worker.Name, ResourcePath = AppHostProjects.Worker.Path)]
partial class WorkerResourceKit
{
    protected override IResourceBuilder<NodeAppResource> BuildResource(IDistributedApplicationBuilder builder)
        => builder.AddNodeApp(Name, AppHostProjects.Worker.Path, "server.js");
}
```

### Aspire 17 Project V2 (recommended on 17)

```csharp
using Aspire.Hosting.Dotnet;

[ResourceDefinition<DotnetProjectResource>("api", ProjectPath = "../Api/Api.csproj")]
partial class ApiResourceKit;
```

With no override, ResourceKit generates `BuildResource` as `AddDotnetProject(builder, Name, path)`.
Write the override only when registration needs more, such as the `configure` callback or a fluent
call on the result. If an override only repeats the generated call, `SG0044` reports it, and its code
fix deletes it, moving the path into `ProjectPath` first if the attribute has none. The generator never
replaces a hand-written override or one a base class already implements.

In a hand-written override, `ProjectPath` is validated against the constant passed to
`AddDotnetProject` after normalising `\` and `/`. A computed path is accepted because it can't be
compared at compile time. Use
`ProjectPath` only with `DotnetProjectResource`; otherwise `SG0041` reports that the declaration is
ignored. `ResourcePath` is the general form for the same project and for JavaScript app resources; when
both are set `ResourcePath` wins and a mismatch is `SG0042`, while a `ResourcePath` on any resource that
is not added by path is `SG0043`.

### Aspire 13.6 legacy form (still works on 17, with `CS0618`)

Use the Aspire-generated project reference type directly in the attribute. When the type argument
implements `IProjectMetadata`, ResourceKit validates it, maps it to `ProjectResource` for the
generated base, and enables two execution-only rules:

- **`SG0018`** — `BuildResource` must register the **declared** project via `AddProject<T>()` (or
  `AddDotnetProject` for V2). Building something else (`builder.AddContainer("api", "my-image")`) is a
  warning, because at runtime the declared project is never registered and the resource fails.
- **`SG0019`** — a project resource kit with an explicit base must resolve to `ProjectResource` (or
  `DotnetProjectResource` for V2).

Both are warnings rather than errors on purpose: generation still proceeds, so you can finish the
override instead of losing the host kit output.

## Enablement gates both phases

Before `BuildResource(...)` runs, the configured `Mode` (`All`, `LocalDevelopment`, `Run`, `Publish`) is
evaluated first, then `IsResourceEnabled(builder)` — **only when `IsEnabled` is already `true`** — and
the combined result is assigned back to `IsEnabled`. A `false` result skips both `BuildResource(...)`
and `ConfigureResource()`. The `purview-resourcekit-options` skill covers the full model, including why
a kit disabled through options is never re-enabled by the hook.

## Groups and dashboard URLs

Group related kits with a `[ResourceGroup]` class, and add a kit to a group with `[InGroup<TGroup>]`. A
kit can be in several groups, and its first group, or the one marked `Primary = true`, is where the
dashboard nests it:

```csharp
[ResourceGroup("platform")] sealed partial class PlatformGroup;
[ResourceGroup<PlatformGroup>("data")] sealed partial class DataGroup;  // or Parent = typeof(PlatformGroup)

[ResourceDefinition<AzurePostgresFlexibleServerResource>("postgres")]
[InGroup<DataGroup>]
[ResourceUrl("Docs", "https://example.com/postgres")]   // absolute: shown as-is
sealed partial class PostgresKit { /* ... */ }

[ResourceDefinition<Projects.Example_Service>("api")]
[ResourceUrl("Health", "/health", DisplayText = "Health")]  // one URL, against https then http
[EndpointUrl("https", DisplayText = "API")]                  // renames the endpoint's own URL
sealed partial class ApiKit { /* ... */ }
```

- A group class must be `partial`, non-generic and must have no base type: the generator derives it from
  `ResourceKitGroup<THostKit>` (`SG0025`, `SG0030`). A class is a kit or a group, never both (`SG0031`), and a
  parent chain can never loop back (`SG0022`).
- Declare a group at **namespace scope**, never nested inside another class, and give it the **same
  accessibility as the host kit** (`SG0045`, `SG0046`). The generator emits the group beside the host kit as
  a second partial declaration, so a nested group's `*Options` path no longer resolves, and an accessibility
  mismatch surfaces as CS9338/CS0051 (`public` group, `internal` host kit) or CS0053 (the inverse).
- In run mode, at the end of `Configure`, each group with a built kit is added as a `ResourceKitGroupResource`
  node, named after the group, or `<name>-group` when a resource already has that name. The node is
  excluded from the manifest. A kit whose resource already has a parent (its own
  `WithParentRelationship`, or an integration such as AspireC4 that builds its own tree) keeps it, and the
  top of that chain is nested in the group instead, unless the chain ends at another kit or a group node.
- A resource an integration adds beside a kit's own (DbGate, pgweb) isn't a kit. Place it with
  `sql.WithDbGate(dbGate => dbGate.WithResourceGroup(HostKit.Groups.Persistence))`. `WithDbGate` returns the SQL builder,
  so the callback is the only place to reach DbGate's.
- Prefer `[ResourceUrl]` to `WithUrl("/path")`. Aspire's relative `WithUrl` rewrites the path of
  **every** endpoint URL; `[ResourceUrl]` adds one URL.
- `HostKit.Groups.Data.Resources` lists a group's kits for cross-resource wiring in `ConfigureResource`.
- Manual host kits pass groups to `AddResource(kit, group, ...)` and override `Urls`.

## Growing a composition

Start with one resource kit, add one class per resource, and use the generated options to toggle
resources for local, dev and test scenarios. Prefer a resource-level toggle to conditional host code
— it keeps the model declarative and testable.

## Related skills

`purview-resourcekit-options` for generated options, enablement and `OptionsHelper`;
`purview-resourcekit-diagnostics` for the `SG00xx` catalogue; `purview-resourcekit-testing` for test
hosts; and `purview-resourcekit-migration` for converting an existing inline AppHost.

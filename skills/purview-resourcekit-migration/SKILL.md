---
name: purview-resourcekit-migration
description: "Use when converting an Aspire AppHost (typically Program.cs/AppHost.cs) into Purview.Aspire.ResourceKit patterns: generate a HostKit, split resources into ResourceKits with BuildResource/ConfigureResource, extract parent/child kits with Parent = typeof(TParentKit), map existing config into generated options (IsEnabled/Name), replace direct wiring with AddAspireResourceKit, and update tests/fixtures/configuration to use OptionsHelper and ResourceKit semantics."
metadata:
  product: Purview.Aspire.ResourceKit
  repository: purview-dev/aspire-resourcekit
---

# Aspire AppHost → ResourceKit Migration Skill

Use this skill to migrate an existing Aspire AppHost composition flow into `Purview.Aspire.ResourceKit` with minimal behavior drift.

This skill supports both small and large AppHost files, including mode-conditional composition (`IsPublishMode`/`IsRunMode`), infrastructure customization, container/deployment pipelines, and local-only dev resources.

## Outcomes

By the end of migration:

1. AppHost composition is represented by a generated host kit (`[HostKit]`).
2. Individual resources are represented by resource kits (`[ResourceDefinition<TResource>]`).
3. Resource construction is in `BuildResource(...)`; inter-resource wiring is in `ConfigureResource()`.
4. Existing enablement/configuration is transposed into generated options (`Name`, `IsEnabled`, etc.).
5. App entry point calls `builder.AddAspireResourceKit(...)` instead of manually composing all resources.
6. Existing tests/config are updated (where possible) to assert ResourceKit behavior.

## Mandatory migration principles

- Preserve runtime behavior first; optimize structure second.
- Prefer incremental commits/patches and verify after each phase.
- Keep `BuildResource(...)` pure to resource construction; place cross-resource references in `ConfigureResource()`.
- Use `IsEnabled` for static/config-driven toggles.
- Use `Mode` (`ResourceKitMode`) for publish-only, run-only or local-development-only resources.
- Use `IsResourceEnabled(builder)` for runtime-state decisions that are not a mode (feature probes, dynamic configuration).
- If `Mode`, `IsEnabled` or `IsResourceEnabled(builder)` excludes the resource, both build and configure paths are skipped for it.

## Discovery checklist

Before changing code, inspect:

- AppHost entrypoint (`Program.cs` or `AppHost.cs`).
- Existing resource registrations (`AddProject`/`AddDotnetProject`, `AddContainer`, `AddRedis`, `AddAzure*`, etc.).
- Cross-resource calls (`WithReference`, endpoints, parent/child wiring).
- Conditional logic (`if`, environment checks, flags) currently gating resources.
- Existing `appsettings*.json` and CLI arg usage.
- Existing test fixtures and integration tests for resource names/enablement.

## Migration algorithm

### 1) Create/identify host kit

- Add a partial host class with `[HostKit]` (for example `ExampleHostKit`).
- Keep placement aligned with repository conventions (for example `AppModels/`).
- If you need deterministic extension naming, set `ExtensionMethodName` on `[HostKit]`.

### 2) Split resource definitions

For each logical resource currently created inline in AppHost:

- Create a partial class with `[ResourceDefinition<TResource>("resource-name")]`.
- Implement `BuildResource(IDistributedApplicationBuilder builder)` with the original creation call.
  The exception is a plain `AddDotnetProject(name, path)`, `AddViteApp(name, path)` or
  `AddNextJsApp(name, path)`: declare the path on the attribute instead and write no override (see 2.0.1).
- Do **not** move cross-resource links into build unless required by API constraints.

Example shape:

```csharp
[ResourceDefinition<ProjectResource>("api")]
partial class ApiResourceKit
{
    protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder)
        => builder.AddProject<Projects.Example_Service>(Name);
}
```

#### 2.0.1) Aspire 17: migrate `AddProject` to Project V2

On an Aspire 17 AppHost, `AddProject<T>` is obsolete (`CS0618`) and the AppHost SDK is gone.

> [!IMPORTANT]
> **Inventory the project references before you change the SDK.** `Aspire.AppHost.Sdk` set
> `IsAspireProjectResource=true` on every `<ProjectReference>`, and `Aspire.Hosting.AppHost` generates
> `Projects.<ClassName>` only from references that carry it. `Microsoft.NET.Sdk` sets no such default, so
> after the swap every plain `<ProjectReference>` silently stops producing its `Projects.*` type. That
> reference is the only record of the project's path, so dropping it, or converting the kit without
> carrying it over, loses the project entirely.

Before editing the csproj, list every `<ProjectReference>` that does **not** say
`IsAspireProjectResource="false"`. Each one was a project resource. Record its `Include` path and its
`Projects.*` class name: the file name without extension, with every non-word character (`.`, `-`) replaced by `_` (or the
`AspireProjectMetadataTypeName` metadata, when set). Then, for **every** entry, do one of these:

1. **Convert it (the default).** Add a `<ResourceKitProject Include="<same path>" Name="<resource name>"
   ClassName="<PascalCase name>" />`, remove the `<ProjectReference>`, and declare
   `ProjectPath = AppHostProjects.<ClassName>.Path` on the kit (example below).
2. **Keep the `Projects.*` type.** Do this when the registration has no path-based Aspire 17 equivalent,
   for example `AddAzureFunctionsProject<T>` or another integration that takes `IProjectMetadata`. Keep the
   reference and restore the metadata the SDK used to add:

   ```xml
   <ProjectReference Include="..\SyncSupportCalendar\SyncSupportCalendar.csproj"
                     IsAspireProjectResource="true"
                     ReferenceOutputAssembly="false"
                     SkipGetTargetFrameworkProperties="true" />
   ```

Afterwards, every `Projects.*` and `AppHostProjects.*` symbol a kit references must resolve, and the
AppHost must build. A `CS0234`/`CS0103` on `Projects.X` or `AppHostProjects.X` means an inventory entry
was dropped. Restore it from the pre-migration csproj (`git show HEAD:<apphost>.csproj`) rather than
deleting the kit.

Convert each entry to `DotnetProjectResource` and declare the path once on the attribute:

```csharp
using Aspire.Hosting.Dotnet;

// No BuildResource: ResourceKit generates AddDotnetProject(Name, "../Api/Api.csproj").
[ResourceDefinition<DotnetProjectResource>("api", ProjectPath = "../Api/Api.csproj")]
partial class ApiResourceKit;
```

- Keep the resource `Name`, and move every `WithReference`/`WithEnvironment` call into
  `ConfigureResource`. `DotnetProjectResource` supports them.
- A kit that already has `BuildResource => builder.AddDotnetProject(Name, path)` reports SG0044. Apply
  its code fix rather than editing by hand: it deletes the override, and moves the path into
  `ProjectPath` first when the attribute has none.
- Keep the override only when registration needs more than the name and path. `ProjectPath` is then
  validated against the constant passed to `AddDotnetProject` after normalising `\` and `/` (SG0018).
  A computed path is accepted but can't be validated. `ResourcePath` is the general
  form for the same project and for JavaScript apps. A Vite or Next.js kit with the default `"dev"`
  script gets its `BuildResource` generated too. A Node, Bun or Deno app needs a script path, so it keeps
  a hand-written override that SG0018 validates. A conflicting pair is SG0042 and a non-path resource is
  SG0043.
- Declare paths through `<ResourceKitProject>` items so the attribute can use
  `AppHostProjects.Api.Path` instead of a string literal.
- On Aspire 13.6, keep the legacy `Projects.*` form; the package still declares the 13.6 floor.
- Preserve the resource name. A legacy kit with no explicit name took it from the class name. Pass that
  same name as the `ResourceKitProject` `Name`, so configuration keys, `WithReference` connection names
  and tests keep working.

#### 2.1) Extract a child resource created from another resource

When the existing AppHost creates a resource from another resource's builder — `vnet.AddSubnet(...)`,
`server.AddDatabase(...)`, `storage.AddBlobs(...)`, `namespace.AddTopic(...)` — model it as a child kit
instead of keeping both responsibilities in one class:

1. Keep the parent creation in the parent `BuildResource`.
2. Remove the child creation from the parent.
3. Add `Parent = typeof(TParentKit)` (and `PropertyName` when the class name is not the property name you
   want) to the child `[ResourceDefinition]`.
4. Build the child with `Parent.Add...` (a resource kit is already an `IResourceBuilder<TResource>`; SG0040
   reports the redundant `Parent.ResourceBuilder` form), and return that builder.
5. Use `Parent` or the generated `hostKit.Parent.Child` from `ConfigureResource`.
6. Guard cross-resource wiring with `HostKit.Parent.IsEnabled && HostKit.Parent.Child.IsEnabled`, because a
   disabled ancestor disables the child.

```csharp
[ResourceDefinition<AzureVirtualNetworkResource>("vnet")]
sealed partial class VnetKit { /* builder.AddAzureVirtualNetwork(Name) */ }

[ResourceDefinition<AzureSubnetResource>("snet-web", PropertyName = "Web", Parent = typeof(VnetKit))]
sealed partial class SubnetWebKit
{
    protected override IResourceBuilder<AzureSubnetResource> BuildResource(
        IDistributedApplicationBuilder builder) =>
        Parent.AddSubnet(Name, "10.0.1.0/24");
}
```

Move the old child config key from `{Host}:{Child}:IsEnabled` to the nested
`{Host}:{Parent}:{Child}:IsEnabled`. See `purview-resourcekit-authoring` for the full hierarchy rules.

### 3) Move wiring to configure

For dependencies/references among resources:

- Implement/extend `ConfigureResource()`.
- Use host/resource properties to link resources after construction.
- Keep order-agnostic assumptions explicit; all enabled resources are built before configure runs.

For long AppHosts, apply this split consistently:

- Keep in `BuildResource(...)`: `Add*`, `PublishAs*`, `RunAs*`, `ConfigureInfrastructure(...)`, endpoint creation, and parameter declarations that are intrinsic to the resource.
- Move to `ConfigureResource()`: `WithReference`, `WithLikeC4Reference`, `WaitFor`, `WaitForCompletion`, and cross-resource environment wiring (for example `WithEnvironment("API_URL", other.GetEndpoint("http"))`).

### 4) Transpose configuration to generated options

Map old configuration into generated options conventions:

- Resource naming → `{HostKitOptionsSection}:{ResourceProperty}:Name`
- Enable/disable flags → `{HostKitOptionsSection}:{ResourceProperty}:IsEnabled`
- Child resources → `{HostKitOptionsSection}:{Parent}:{Child}:{Name|IsEnabled|Mode}`

If tests or bootstrap code currently pass args directly, convert to `OptionsHelper.Assign<THostOptions>(...)`:

```csharp
var args = OptionsHelper.Assign<ExampleHostKit.ExampleHostKitOptions>(
    c => c.Redis.IsEnabled = false,
    c => c.AzureStorage.Name = "custom-storage"
).Build();
```

### 4.1) Extend generated typed options (host + resource)

Generated options are `sealed partial` nested types, so you can extend them with additional configuration fields.

- Extend host-level options by adding a partial for `{HostKitType}.{HostKitType}Options`.
- Extend resource-level options by adding a partial for `{ResourceKitType}.{ResourceKitType}Options`.
- Keep extension members bindable (`public` + `get; set;`) and supply sensible defaults.

Resource-level extension example:

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

Host-level extension example:

```csharp
[HostKit]
partial class ExampleHostKit
{
  partial class ExampleHostKitOptions
  {
    public bool EnablePreviewResources { get; set; }
  }
}
```

Use these properties via generated `Options` surfaces:

- `HostKit.Options` for host-level values.
- `Options` inside each resource kit for resource-level values.

### 5) Convert conditional resource logic

Translate existing conditional logic as follows:

- Config-only conditions → default to `IsEnabled` option values.
- Publish/run/local-development conditions → `ResourceKitMode` on the attribute (or a `Mode` config override).
- Other runtime conditions → override `IsResourceEnabled(builder)`.

Pattern:

```csharp
protected override bool IsResourceEnabled(IDistributedApplicationBuilder builder)
{
    var enabledFromOptions = IsEnabled;
    var isProduction = builder.Configuration["ASPNETCORE_ENVIRONMENT"] == "Production";

    return enabledFromOptions && isProduction;
}
```

When AppHost has multiple mode branches, map each branch into targeted resource kits with branch-aware enablement:

- Publish-only resources: `[ResourceDefinition<T>(ResourceKitMode.Publish)]`.
- Run-only resources: `[ResourceDefinition<T>(ResourceKitMode.Run)]`.
- Local-development-only resources (docs sites, diagram sites): `[ResourceDefinition<T>(ResourceKitMode.LocalDevelopment)]`.
- Test-mode-only behavior: include config predicates such as `builder.Configuration.GetValue<bool>("Testing:IsTestMode")`.

### 6) Update AppHost entrypoint

Replace manual resource composition with generated registration:

- `builder.AddAspireResourceKit();` when default generated extension name is used.
- `builder.AddAspireResourceKit<YourHostKit>();` when explicit host type registration is preferred.

Then keep the standard build/run flow.

### 7) Update tests and fixtures (attempt required)

When tests/config exist, migrate them to ResourceKit semantics:

- Replace direct key/value arg strings with `OptionsHelper.Assign<THostOptions>(...)` where practical.
- Update assertions to verify:
  - resources disabled via options are absent/inaccessible,
  - custom names flow through to produced resources,
  - configured wiring still behaves as before.
- Keep testing conventions used by this repository

### 8) Update runtime config files and docs (attempt required)

- Preserve existing `appsettings*.json` values by mapping to generated options section paths.
- Keep environment-specific overrides working (`appsettings.Development.json`, CLI args, fixture args).
- Update local docs/README snippets if they describe old inline AppHost composition.
- Include any new custom option keys added through partial options extensions.

## Complex AppHost mapping guide (for long Program.cs files)

Use this deterministic decomposition for large, linear AppHost scripts.

### A) Partition by ownership first

Create one resource kit per owned resource or tightly-coupled group:

- Registry/environment bootstrap (for example ACR + ACA environment)
- Storage account (+ blobs)
- SQL server (+ database)
- Web app/project host
- Migrations worker (bundle/init container)
- Observability/secrets (App Insights + Key Vault)
- Dev-only tools (Mailpit, Scalar)
- Frontend/landing dev servers

### B) Classify statements by lifecycle phase

1. **Construct (Build/BuildResource)**
   - resource creation (`AddAzureStorage`, `AddAzureSqlServer`, `AddProject`, `AddViteApp`, etc.)
   - intrinsic publish/runtime setup (`PublishAsDockerFile`, `PublishAsAzureContainerApp`, `RunAsContainer`, `RunAsEmulator`)
   - infra shaping (`ConfigureInfrastructure(...)`, ARM SKU/TLS/MaxSize settings)
   - parameter creation used by the same resource (`AddParameter(...)`)

2. **Attach (Configure/ConfigureResource)**
   - `WithReference(...)`
   - `WithLikeC4Reference(...)`
   - `WaitFor(...)`, `WaitForCompletion(...)`
   - cross-resource endpoint/env hookups

### C) Handle repeated blocks once

If publish and run branches both create similar resources (for example migrations container):

- Prefer one `MigrationsResourceKit` with options such as:
  - `UseInPublishMode` (default `true`)
  - `UseInRunMode` (default from config)
  - `UseBundleInRunMode` (maps `Migrations:UseBundle`)
- Implement branch logic in `IsResourceEnabled(builder)` and/or `ConfigureResource()`.
- Keep duplication out of AppHost entrypoint.

### D) Preserve advanced infra/deployment customization

For code like `ConfigureInfrastructure(static infra => ...)` and `PublishAsAzureContainerApp((_, app) => ...)`:

- Keep these lambdas inside the owning resource's `BuildResource(...)`.
- Move magic constants into typed options when they are likely environment-specific:
  - scale limits,
  - domain names / certificate parameter defaults,
  - connection labels/summaries,
  - SQL SKU / size policy toggles.

### E) Options transposition for large apps

Promote hard-coded values to typed options incrementally:

- Host-level toggles (global behavior): `ExampleHostKitOptions`
  - `EnablePublishCustomDomains`, `EnableScalarInRunMode`, `IsTestModeOverride`, etc.
- Resource-level options (resource behavior): `{ResourceKit}Options`
  - names, image tags, volume names, scale settings, domain/cert defaults.

Then wire tests/fixtures with:

```csharp
OptionsHelper.Assign<ExampleHostKit.ExampleHostKitOptions>(
    c => c.Sql.IsEnabled = true,
    c => c.Migrations.UseBundleInRunMode = true
).Build();
```

### F) End-state entrypoint shape

After migration, `Program.cs` should be mostly:

1. `var builder = DistributedApplication.CreateBuilder(args);`
2. optional non-resource host setup (for example title/banner)
3. `builder.AddAspireResourceKit(...);`
4. build/run

All resource construction and wiring should live in HostKit/ResourceKit classes.

## Safety and compatibility notes

- Prefer `[ResourceDefinition<TResource>]` for new conversions; avoid mixing generic and non-generic forms on the same class.
- Do not introduce non-TUnit frameworks in this repository.
- Keep generated-options usage section-scoped (not root-bound) to avoid lost values.
- Preserve existing resource names unless there is an intentional rename.

## Verification checklist

Run and confirm:

1. Build succeeds for the AppHost and related projects.
2. Relevant integration/unit tests pass.
3. Resource enablement toggles behave the same or better than before.
4. AppHost startup still builds and runs with expected resources.

## Definition of done

Migration is complete when:

- HostKit + ResourceKits fully replace inline AppHost composition,
- Build/configure responsibilities are separated correctly,
- options and runtime enablement behavior are preserved,
- AppHost uses generated extension wiring,
- and affected tests/config are updated and passing.

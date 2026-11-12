---
name: purview-resourcekit-diagnostics
description: >-
  Use when a Purview.Aspire.ResourceKit build reports an SG00xx diagnostic - what triggered it, how to
  fix it, which rules block source generation and which are execution-only warnings, and why CS8618 is
  suppressed on IResourceBuilder properties.
metadata:
  product: Purview.Aspire.ResourceKit
  repository: purview-dev/aspire-resourcekit
---

# ResourceKit diagnostics

`ResourceKitDiagnosticAnalyzer` evaluates the `SGxxxx` rules, and the source generator uses the same
shared rule set to decide what it can generate.

ResourceKit also ships `ResourceKitDiagnosticSuppressor`, which suppresses **`CS8618`** for
non-nullable `IResourceBuilder<T>` properties on resource kits. Those are populated at runtime during
the `BuildResource`/`ConfigureResource` lifecycle, so "must contain a non-null value when exiting the
constructor" does not apply — and `SG0017` is the rule that actually tells you a property is never
assigned.

## The catalogue

| ID | Severity | Meaning |
| --- | --- | --- |
| `SG0001` | Error | A participating class is not `partial`. |
| `SG0002` | Info | A host kit exists but no resources were defined. |
| `SG0003` | Warning | Resources exist but no host kit was defined. |
| `SG0004` | Error | More than one `[HostKit]` class. |
| `SG0005` | Error | Two resources map to the same generated property name. |
| `SG0006` | Error | A resource does not derive from the expected generated base. |
| `SG0007` | Error | The resource name could not be inferred and `Name` was not set. |
| `SG0008` | Error | An explicit `PropertyName` is not a valid C# identifier. |
| `SG0009` | Error | Missing `IServiceCollection` dependency. |
| `SG0010` | Error | Missing configuration binder dependency. |
| `SG0011` | Error | Missing options configuration extensions dependency. |
| `SG0012` | Error | A non-empty constructor on an attributed class. |
| `SG0013` | Error | `ResourceDefinition` and `ResourceDefinition<TResource>` mixed on one class. |
| `SG0014` | Error | Non-generic `ResourceDefinition` without an explicit compatible base. |
| `SG0015` | Error | Generic `ResourceDefinition<TResource>` with an explicit base. |
| `SG0016` | Error | No Aspire resource type could be inferred or found. |
| `SG0017` | Warning | An `IResourceBuilder<T>` property is never assigned in `BuildResource` or `ConfigureResource`. |
| `SG0018` | Warning | A project resource kit does not add the declared project via `AddProject<T>()`. |
| `SG0019` | Warning | A project resource kit's explicit base does not use `ProjectResource`. |
| `SG0020` | Error | An `OptionsHelper.Assign` action sets more than one property path. |
| `SG0021` | Error | An `[InGroup<TGroup>]` type, or a group's `Parent`, is not a `[ResourceGroup]` class in this compilation. |
| `SG0022` | Error | A resource group is its own ancestor. |
| `SG0023` | Error | Two groups share a name (case-insensitive, as the dashboard compares names) or a property name. |
| `SG0024` | Warning | A kit marks more than one group `Primary`; the first is used. |
| `SG0025` | Error | A `[ResourceGroup]` class declares a base type. |
| `SG0026` | Error | Two URLs on one kit or group share a name. |
| `SG0027` | Error | An `[EndpointUrl]`, or a URL with an `Endpoint`, on a resource or group without endpoints. |
| `SG0028` | Warning | A relative URL on a resource or group without endpoints; it is skipped at runtime. |
| `SG0029` | Error | A group declares its parent more than once (`ResourceGroup<TParent>` and `Parent`, or both attributes). |
| `SG0030` | Error | A group is generic, or nested in a generic type. |
| `SG0031` | Error | A group is also a resource kit, or carries `[InGroup]`. |
| `SG0032` | Error | A resource kit declares itself as its parent. |
| `SG0033` | Error | The parent chain contains a cycle; the message prints the path. |
| `SG0034` | Error | The declared parent type is not a resource kit. |
| `SG0035` | Error | The declared parent is not part of this host kit composition. |
| `SG0036` | Error | The generated child property collides with a member on the parent. |
| `SG0037` | Error | Two children of one parent use the same generated property name. |
| `SG0038` | Error | The generated `Parent` member collides with a user-authored member. |
| `SG0039` | Warning | A child's explicit primary group differs from its parent's; visual placement follows the child's group. |
| `SG0040` | Info | A resource kit is already an `IResourceBuilder<TResource>`; `.ResourceBuilder` is redundant. |

## Execution-only versus generation-blocking

`SG0017`, `SG0018`, `SG0019`, `SG0024` and `SG0028` are **execution-only**. They report things that break the resource at
runtime — an unset builder property, a project that is never registered, a base that cannot build a
project — but they deliberately do **not** stop generation, and are warnings for that reason.

The point is practical: a resource kit whose `BuildResource` does not yet register its declared
project is still generated, and the host kit still emitted, so you can finish the override instead of
losing the whole output to a half-written method.

Only the Error-severity rules block generation.

## Fast triage

1. Is every host/resource class `partial`?
2. Is there exactly one `[HostKit]` in the compilation?
3. Are the generated property names unique?
4. Does each resource use one attribute style, with a matching base?
5. Is `Name` set where inference cannot determine it?

## Common fixes

### `SG0005` — duplicate generated property names

```csharp
[ResourceDefinition<ProjectResource>("api", PropertyName = "Api")]
[ResourceDefinition<ProjectResource>("admin", PropertyName = "AdminApi")]
```

### `SG0008` — invalid `PropertyName`

Valid C# identifiers only: `Api`, `RedisCache`, `OrderDb`.

### `SG0004` — multiple host kits

One `[HostKit]` per compilation. Split scenarios into separate projects.

### `SG0013` / `SG0014` / `SG0015` — attribute style and base type

One style per class. Non-generic `[ResourceDefinition("name")]` needs an explicit compatible base;
generic `[ResourceDefinition<TResource>("name")]` must **not** declare one.

### `SG0017` — unassigned `IResourceBuilder<T>`

Every such property, nullable or not, must be assigned in `BuildResource` or `ConfigureResource`:

```csharp
[ResourceDefinition<AzureSqlServerResource>("sql")]
sealed partial class SqlServerKit
{
    public IResourceBuilder<AzureSqlDatabaseResource> Database { get; private set; }

    protected override void ConfigureResource()
    {
        Database = ResourceBuilder.AddDatabase("changeops-db", "ChangeOps");
        base.ConfigureResource();
    }
}
```

### `SG0021`–`SG0031` — groups and URLs

- `SG0021`: the type in `[InGroup<T>]`, or in `Parent = typeof(T)`, must be a `[ResourceGroup]` class declared
  in the same project.
- `SG0022` / `SG0031`: a group can never contain itself. Nest groups through one parent, as
  `[ResourceGroup<TParent>]` or `Parent = typeof(TParent)` (not both: `SG0029`), and keep kits and groups as
  separate classes.
- `SG0025`: remove the base type from the group class; the generator supplies `ResourceKitGroup<THostKit>`.
- `SG0026`: give each `[ResourceUrl]` a unique name. `[EndpointUrl]` takes its name from the endpoint, so two
  `[EndpointUrl("https")]` also collide.
- `SG0027` / `SG0028`: a group, or a kit for a resource without endpoints such as a `ParameterResource`, can
  only show an **absolute** URL.

### `SG0032`–`SG0038` — parent/child hierarchies

- `SG0032` / `SG0033`: a kit can never be its own parent or form a parent chain that loops. Move the
  intended root up the chain. The cycle diagnostic prints the path, for example
  `PostgresKit -> DatabaseKit -> MigrationKit -> PostgresKit`.
- `SG0034` / `SG0035`: `Parent = typeof(T)` must name a `[ResourceDefinition]` kit in the same compilation
  and host composition.
- `SG0036` / `SG0038`: the generated child property or `Parent` accessor collides with a member you already
  declared. Rename the member or set a different `PropertyName`.
- `SG0037`: child property names must be unique **within one parent**. Two different parents can both have a
  `Database` child.

Manual graphs are validated at runtime before any Aspire resource is created; a self-parent, cycle, missing
parent or foreign-host parent throws `InvalidOperationException` even when the generator was not involved.

`SG0039` is the group/hierarchy warning: a child with no `[InGroup]` inherits its parent's primary group, but
an explicit child group overrides that. ResourceKit parentage does not add an Aspire visual parent, so the
child then renders in its own group; if an Aspire parent relationship exists, it renders under the parent and
the child's group is not where it appears. Use the same primary group or accept the placement deliberately.

`SG0040` reports `Parent.ResourceBuilder.AddDatabase(...)` because a kit is already an `IResourceBuilder`.
Use `Parent.AddDatabase(...)`. The "Use the resource kit directly" code fix removes the hop. It only offers
the fix when the kit is the receiver of the next call, not when the builder is passed as an argument and the
expected parameter type could differ.

### `SG0020` — one property path per `Assign` action

```csharp
// SG0020
OptionsHelper.Assign<ShopHostKitOptions>(o =>
{
    o.API.IsEnabled = false;
    o.API.Name = "api-test";
});

// Correct
OptionsHelper.Assign<ShopHostKitOptions>(
    o => o.API.IsEnabled = false,
    o => o.API.Name = "api-test"
);
```

Visual Studio offers a **"Split into separate assignments"** code fix.

## Related skills

`purview-resourcekit-authoring` for the attribute and lifecycle rules behind most of these, and
`purview-resourcekit-options` for `SG0020` and `OptionsHelper`.

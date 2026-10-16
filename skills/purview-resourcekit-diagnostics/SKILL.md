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

## Execution-only versus generation-blocking

`SG0017`, `SG0018` and `SG0019` are **execution-only**. They report things that break the resource at
runtime — an unset builder property, a project that is never registered, a base that cannot build a
project — but they deliberately do **not** stop generation, and are warnings for that reason.

The point is practical: a resource kit whose `BuildResource` does not yet register its declared
project is still generated, and the host kit still emitted, so you can finish the override instead of
losing the whole output to a half-written method.

Only the Error-severity rules, `SG0001`–`SG0016`, block generation.

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

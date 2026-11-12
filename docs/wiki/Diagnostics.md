# Diagnostics

ResourceKit reports diagnostics with `SGxxxx` IDs to help you fix model issues quickly. The rules are
evaluated by the bundled `ResourceKitDiagnosticAnalyzer` (and the source generator uses the same shared
rule set to decide what can be generated).

ResourceKit also ships `ResourceKitDiagnosticSuppressor`, which automatically suppresses `CS8618` for
non-nullable `IResourceBuilder<T>` properties declared on resource kits — these are populated at runtime
during the `BuildResource`/`ConfigureResource` lifecycle, so the "must contain a non-null value when
exiting the constructor" warning does not apply.

## Diagnostic reference

| ID | Severity | What it means |
| - | - | - |
| SG0001 | Error | A participating class must be `partial` |
| SG0002 | Info | Host kit exists but no resources were defined |
| SG0003 | Warning | Resources exist but no host kit was defined |
| SG0004 | Error | More than one `[HostKit]` class was found |
| SG0005 | Error | Two resources map to the same generated property name |
| SG0006 | Error | A resource does not derive from the expected generated base |
| SG0007 | Error | Resource name could not be inferred and `Name` was not set |
| SG0008 | Error | Explicit `PropertyName` is not a valid C# identifier |
| SG0009 | Error | Missing `IServiceCollection` dependency |
| SG0010 | Error | Missing configuration binder dependency |
| SG0011 | Error | Missing options configuration extensions dependency |
| SG0012 | Error | Non-empty constructors are not supported on attributed classes |
| SG0013 | Error | Mixed `ResourceDefinition` and `ResourceDefinition<TResource>` usage on one class |
| SG0014 | Error | Non-generic `ResourceDefinition` requires explicit compatible base type |
| SG0015 | Error | Generic `ResourceDefinition<TResource>` cannot declare explicit base type |
| SG0016 | Error | No Aspire resource type could be inferred/found |
| SG0017 | Warning | An `IResourceBuilder<T>` property is never assigned in `BuildResource` or `ConfigureResource` |
| SG0018 | Warning | A project resource kit does not add the declared project via `AddProject<T>()` |
| SG0019 | Warning | A project resource kit declares an explicit base class that does not use `ProjectResource` |
| SG0020 | Error | An `OptionsHelper.Assign` action sets more than one property path |
| SG0021 | Error | An `[InGroup<TGroup>]` type argument, or a group's `Parent`, is not a `[ResourceGroup]` class |
| SG0022 | Error | A resource group is its own ancestor |
| SG0023 | Error | Two resource groups share a name (case-insensitive) or a property name |
| SG0024 | Warning | A resource kit marks more than one group as `Primary` |
| SG0025 | Error | A resource group declares a base type |
| SG0026 | Error | Two URLs on one resource kit or group share a name |
| SG0027 | Error | An endpoint URL is declared on a resource, or group, without endpoints |
| SG0028 | Warning | A relative URL is declared on a resource, or group, without endpoints |
| SG0029 | Error | A resource group declares its parent more than once |
| SG0030 | Error | A resource group is generic, or nested in a generic type |
| SG0031 | Error | A resource group is also a resource kit, or carries `[InGroup]` |
| SG0032 | Error | A resource kit declares itself as its parent |
| SG0033 | Error | The parent chain contains a cycle; the message prints the path |
| SG0034 | Error | The declared parent type is not a resource kit |
| SG0035 | Error | The declared parent is not part of this host kit composition |
| SG0036 | Error | The generated child property collides with a member on the parent |
| SG0037 | Error | Two children of the same parent use the same generated property name |
| SG0038 | Error | The generated `Parent` member collides with a user-authored member |
| SG0039 | Warning | A child uses an explicit primary group different from its parent's; visual placement follows the child's group |
| SG0040 | Info | A resource kit is already an `IResourceBuilder<TResource>`; `.ResourceBuilder` is redundant |

See [Attributes Reference](Attributes-Reference.md) for the rules tied to attribute styles,
[Project Resources](Project-Resources.md) for SG0018/SG0019 specifics,
[Groups and URLs](Groups-and-URLs.md) for SG0021–SG0031, and
[Parent and child resource kits](Hierarchies.md) for SG0032–SG0040.

A parent cycle is reported on one kit in the cycle and includes the full path, for example
`PostgresKit -> DatabaseKit -> MigrationKit -> PostgresKit`. Invalid hierarchies block generation; manual
graphs are validated at runtime before any Aspire resource is created.

## Execution-only vs generation-blocking rules

SG0017, SG0018, SG0019, SG0024 and SG0028 are **execution-only** rules. They report problems that break the resource at
runtime (an unset builder property, a project that is never registered, or a base class that cannot build
a project) but they do **not** prevent source generation. They are reported as warnings so generation
always proceeds — for example, a resource kit whose `BuildResource` does not yet register its declared
project via `AddProject<T>()` is still generated (and the host kit is still emitted) so the user can
complete the override instead of losing the whole output. Only Error-severity rules block generation.
SG0024 keeps the first primary group, and SG0028's URL is skipped at runtime.

## `OptionsHelper.Assign` action with multiple property paths (SG0020)

Each `OptionsHelper.Assign<TOptions>(...)` action must set exactly one property path. A block-bodied lambda
such as the following is rejected at compile time:

```csharp
OptionsHelper.Assign<ShopHostKitOptions>(o =>
{
    o.API.IsEnabled = false;
    o.API.Name = "api-test";
});
```

Split each property into its own assignment argument instead:

```csharp
OptionsHelper.Assign<ShopHostKitOptions>(
    o => o.API.IsEnabled = false,
    o => o.API.Name = "api-test"
);
```

Visual Studio offers a **"Split into separate assignments"** code fix that performs this conversion for you.
See [OptionsHelper](OptionsHelper.md) and [Source Generator Behaviors](Source-Generator-Behaviors.md).

## Redundant `ResourceBuilder` access (SG0040)

A resource kit derives from `ResourceKitBase<THostKit, TResource>`, which implements
`IResourceBuilder<TResource>`. When the kit is the receiver of the next call, the `.ResourceBuilder` hop is
redundant:

```csharp
// Before (SG0040, Info)
Parent.ResourceBuilder.AddDatabase(Name);

// After — "Use the resource kit directly"
Parent.AddDatabase(Name);
```

The code fix only fires when the hop is the receiver of a following member access/invocation. It deliberately
does not fire in argument position, where the expected parameter type might be `IResourceBuilder<IResource>`
and the kit's own `IResourceBuilder<TResource>` would not satisfy it.

## Fast troubleshooting checklist

1. Ensure host/resource classes are marked `partial`.
2. Ensure exactly one `[HostKit]` class exists in the compilation.
3. Ensure root property names are unique, and child property names are unique within each parent
   (explicit `PropertyName` can help).
4. Ensure each resource uses a compatible base and definition attribute style.
5. Set explicit `Name` when inference cannot determine resource name.
6. If the kit declares `Parent = typeof(...)`, ensure that type is a resource kit in the same composition;
   a child in a different primary group than its parent gets SG0039, and a redundant
   `Parent.ResourceBuilder` gets SG0040.

## Common fixes

### Duplicate generated property names (SG0005)

Use unique `PropertyName` values:

```csharp
[ResourceDefinition<ProjectResource>("api", PropertyName = "Api")]
[ResourceDefinition<ProjectResource>("admin", PropertyName = "AdminApi")]
```

### Invalid `PropertyName` (SG0008)

Use valid C# identifiers only (`Api`, `RedisCache`, `OrderDb`, etc.).

### Multiple host kits (SG0004)

Keep one `[HostKit]` per compilation; split scenarios into separate projects if needed.

### Mixed attribute styles (SG0013)

Use exactly one style per class:

- `[ResourceDefinition("name")]`
- or `[ResourceDefinition<TResource>("name")]`

Do not apply both to the same class.

### Base-type mismatch by style (SG0014 / SG0015)

- If you use non-generic `[ResourceDefinition("name")]`, declare an explicit compatible base.
- If you use generic `[ResourceDefinition<TResource>("name")]`, do not declare an explicit base.

### Unassigned `IResourceBuilder<T>` property (SG0017)

Every `IResourceBuilder<T>` property on a resource kit (nullable or not) must be assigned in either
`BuildResource` or `ConfigureResource`. `CS8618` is automatically suppressed for these properties because
they are populated at runtime, so SG0017 is the signal that a property is never set:

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
# Parent and child resource kits

A resource kit can own other resource kits. This splits a large resource definition across focused classes
while keeping the parent's builder available to every descendant, building parents before children, and
disabling a whole subtree when an ancestor is disabled.

Issue [#57](https://github.com/purview-dev/aspire-resourcekit/issues/57) is the production scenario behind
this feature: one VNET and ten subnets no longer belong in one class.

## Declare a parent

Use `Parent = typeof(...)` on `[ResourceDefinition]`:

```csharp
[ResourceDefinition<AzurePostgresFlexibleServerResource>("postgres")]
sealed partial class PostgresKit
{
    protected override IResourceBuilder<AzurePostgresFlexibleServerResource>
        BuildResource(IDistributedApplicationBuilder builder) =>
            builder.AddAzurePostgresFlexibleServer(Name);
}

[ResourceDefinition<AzurePostgresFlexibleServerDatabaseResource>(
    "db",
    PropertyName = "Database",
    Parent = typeof(PostgresKit))]
sealed partial class PostgresDatabaseKit
{
    protected override IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource>
        BuildResource(IDistributedApplicationBuilder builder) =>
            Parent.ResourceBuilder.AddDatabase(Name);
}
```

The generator produces:

```csharp
hostKit.Postgres.Database          // strongly typed child property on the parent
PostgresDatabaseKit.Parent         // strongly typed parent instance on the child
```

`Parent.ResourceBuilder` is available inside `BuildResource`, because the parent has finished building first.
The child returns the resource it created; ResourceKit never creates a second copy.

`PropertyName` is still the child property name, and the default is derived from the class name exactly as
for a root kit. Set it explicitly when the class name is not the property name you want
(`PostgresDatabaseKit` defaults to `PostgresDatabase`; `PropertyName = "Database"` gives `Database`).

## One parent, many children, arbitrary depth

Each kit has zero or one parent. A parent may have any number of children, and every child may own further
children:

```csharp
[ResourceDefinition<AzureVirtualNetworkResource>("vnet")]
sealed partial class VnetKit
{
    protected override IResourceBuilder<AzureVirtualNetworkResource>
        BuildResource(IDistributedApplicationBuilder builder) =>
            builder.AddAzureVirtualNetwork(Name);
}

[ResourceDefinition<AzureSubnetResource>(
    "snet-web",
    PropertyName = "Web",
    Parent = typeof(VnetKit))]
sealed partial class SubnetWebKit
{
    protected override IResourceBuilder<AzureSubnetResource>
        BuildResource(IDistributedApplicationBuilder builder) =>
            Parent.ResourceBuilder.AddSubnet(Name, "10.0.1.0/24");
}

[ResourceDefinition<AzureSubnetResource>(
    "snet-data",
    PropertyName = "Data",
    Parent = typeof(VnetKit))]
sealed partial class SubnetDataKit
{
    protected override IResourceBuilder<AzureSubnetResource>
        BuildResource(IDistributedApplicationBuilder builder) =>
            Parent.ResourceBuilder.AddSubnet(Name, "10.0.2.0/24");
}
```

Then:

```csharp
hostKit.Vnet.Web
hostKit.Vnet.Data
```

Construction and build order are parent-first, in a stable topological order. The generated host constructs
the roots, then each child with its parent instance, validates the whole graph, and only then builds enabled
resources. You never register children by hand for generated kits.

## Manual kits

Manual composition uses the parent-aware base constructor:

```csharp
sealed partial class PostgresDatabaseKit(ShopHostKit hostKit, PostgresKit parent)
    : ResourceKitBase<ShopHostKit, AzurePostgresFlexibleServerDatabaseResource>(
        hostKit,
        parent,
        "db")
{
    public PostgresKit Parent { get; } = parent;

    protected override IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource>
        BuildResource(IDistributedApplicationBuilder builder) =>
            Parent.ResourceBuilder.AddDatabase(Name);
}
```

Register the parent before or after the child; `HostKitBase` validates and orders the graph itself:

```csharp
Postgres = new(this);
PostgresDatabase = new(this, Postgres);
AddResource(Postgres);
AddResource(PostgresDatabase);
```

The graph is validated before any Aspire resource is created. A self-parent, a cycle, an unregistered
parent, a duplicate registration, or a parent from another host throws `InvalidOperationException` with a
useful message.

## Enablement inheritance

A child participates only when every gate passes:

```text
effective =
    host Mode
    AND every group gate
    AND every ancestor's effective enablement
    AND the configured IsEnabled
    AND the kit Mode
    AND IsResourceEnabled(builder)
```

Consequences:

- Disabling a parent disables every descendant. A child cannot re-enable itself with `IsEnabled = true`.
- A parent excluded by its `Mode` excludes every descendant.
- A child excluded by its `Mode`, or by its own hook, leaves the parent and eligible siblings alone.
- A disabled ancestor never invokes a descendant's `IsResourceEnabled` hook.
- Siblings are independent.
- A disabled or unbuilt kit creates no Aspire resource.

`IsEnabled` keeps its historical behaviour: it is the configured value before `Build` and the effective value
afterwards. Two additional read-only properties make the distinction explicit:
`ConfiguredIsEnabled` (the value entering the build phase) and `EffectiveIsEnabled` (the final decision).

`ResourceBuilder` throws `InvalidOperationException` while the kit is disabled, before it has built, or after
a build/configure failure. It never returns a silent `null`.

## Configuration

Root options keep their existing path. Children nest under their parent's options:

```json
{
  "ExampleHostKit": {
    "Postgres": {
      "IsEnabled": true,
      "Name": "postgres",
      "Database": {
        "IsEnabled": true,
        "Name": "db"
      }
    }
  }
}
```

```csharp
public sealed partial class PostgresKitOptions
{
    public bool IsEnabled { get; set; } = true;

    public PostgresDatabaseKit.PostgresDatabaseKitOptions Database { get; init; } = new();
}
```

`OptionsHelper` resolves the nested path:

```csharp
OptionsHelper.Assign<ExampleHostKitOptions>(
    static o => o.Postgres.Database.IsEnabled = false)
    .Build(); // --ExampleHostKit:Postgres:Database:IsEnabled=false
```

Each level has its own `Name`, `IsEnabled` and `Mode`. `Mode` defaults from the attribute, exactly as for a
root kit. With `[HostKit(GenerateOptions = false)]` children still work; the generated host passes the
resource names from the attribute instead of options.

## Failure policy

- `BuildResource` and `ConfigureResource` exceptions propagate unchanged.
- A failed parent aborts the host build; no descendant is built.
- `Configure` skips a kit whose parent did not build or configure successfully.
- A host builds and configures at most once. Repeated calls are no-ops; a failed first build is not
  silently retried.
- Lifecycle orchestration lives in `HostKitBase`, not in parent `BuildResource` methods. A parent never
  recursively calls a child's `Build`.

## Parentage is not a general dependency

ResourceKit parentage means **composition ownership**: construction context, ordering, inherited enablement,
and optional group inheritance. It is not a general resource dependency graph. An API that needs Redis and
PostgreSQL remains an independent kit and wires those references in `ConfigureResource`:

```csharp
protected override void ConfigureResource()
{
    if (HostKit.Redis.IsEnabled)
        ResourceBuilder.WithReference(HostKit.Redis).WaitFor(HostKit.Redis);

    if (HostKit.Postgres.Database.IsEnabled)
        ResourceBuilder.WithReference(HostKit.Postgres.Database).WaitFor(HostKit.Postgres.Database);
}
```

## Groups and visual placement

Three different concepts stay separate:

| Concept | What it controls |
| --- | --- |
| ResourceKit parent/child | Build ordering, parent builder access, inherited enablement |
| `[ResourceGroup]` / `[InGroup]` | Enablement and dashboard grouping |
| Aspire `WithParentRelationship` | The dashboard's single visual parent for a resource |

A child with no `[InGroup]` inherits its parent's **primary** group for membership and dashboard placement.
A child with an explicit `[InGroup]` keeps it instead. Grouping never changes build ordering, and a disabled
parent still disables descendants even when a child's own group is enabled.

ResourceKit does **not** add an Aspire visual parent relationship automatically. If you want a child's
resource to render under the parent's resource, add it explicitly in `BuildResource`:

```csharp
var database = Parent.ResourceBuilder.AddDatabase(Name);
database.WithParentRelationship(Parent.ResourceBuilder);
return database;
```

The group placement rules then respect that existing chain, exactly as they do for any integration that
builds its own resource tree. See [Resource groups and dashboard URLs](Groups-and-URLs.md).

## Diagnostics

| ID | Meaning |
| --- | --- |
| SG0032 | A kit declares itself as its parent |
| SG0033 | The parent chain contains a cycle (the message prints the path) |
| SG0034 | The parent type is not a resource kit |
| SG0035 | The parent is not part of this host kit composition |
| SG0036 | The generated child property collides with a member on the parent |
| SG0037 | Two children of one parent use the same property name |
| SG0038 | The generated `Parent` member collides with a user-authored member |

Invalid graphs block generation; the runtime validates manual graphs before creating any Aspire resource.
See [Diagnostics](Diagnostics.md).

## Compatibility

- Root kits need no changes.
- Existing configuration paths are unchanged.
- Existing `Mode`, `IsEnabled`, `Name`, custom options, custom `PropertyName` and manual kits continue to
  work.
- Groups, URLs and existing parent relationship annotations continue to work.

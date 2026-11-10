# Resource groups and dashboard URLs

Two ways to manage resource kits beyond one kit at a time:

- **Resource groups** gather kits under a name. A group can be switched off, or given a run mode, with one
  setting. In run mode it appears as a node in the Aspire dashboard, with its kits nested underneath.
- **Named URLs** declare the links the dashboard shows for a kit or a group. Options can switch each one
  off or change it.

Both features are built on Aspire APIs (`WithParentRelationship`, `WithUrls`, `ResourceUrlAnnotation`), so
they need no Aspire version beyond the one ResourceKit already requires.

## Resource groups

A group is a class marked `[ResourceGroup]`. A kit joins one or more groups with `[InGroup<TGroup>]`. A group
nests under a parent with `[ResourceGroup<TParent>]`, or with `Parent = typeof(TParent)`; the two forms are
equivalent, and a group may use only one of them (SG0029):

```csharp
[ResourceGroup("platform")]
sealed partial class PlatformGroup;

[ResourceGroup<PlatformGroup>("data")]          // or [ResourceGroup("data", Parent = typeof(PlatformGroup))]
sealed partial class DataGroup;

[ResourceDefinition<AzurePostgresFlexibleServerResource>("postgres")]
[InGroup<DataGroup>]
[InGroup<BackupGroup>]
sealed partial class PostgresKit { /* ... */ }
```

| `[ResourceGroup]` argument | Default | Purpose |
| --- | --- | --- |
| `Name` | Class name without a `ResourceGroup` or `Group` suffix | The group name, unique among groups (SG0023). It also names the group's dashboard node; if a resource already has that name (a `minder` group holding a `minder` database), the node is named `minder-group` instead. |
| `Parent` / `TParent` | none | The parent group: another `[ResourceGroup]` class (SG0021). It must not form a cycle (SG0022), and only one may be given (SG0029). |
| `ShowInDashboard` | `true` | Whether the group appears as a dashboard node. |
| `Mode` | `All` | The run mode in which the group, and therefore every kit in it, takes part. |

The generator derives the group class from `ResourceKitGroup<THostKit>`, so the class must be `partial`
and must not declare a base type (SG0025). It must not be generic (SG0030), and it cannot also be a resource kit or carry
`[InGroup]` (SG0031): groups nest through their parent only. The host kit gets a typed `Groups` accessor:

```csharp
foreach (var resource in HostKit.Groups.Data.Resources.Where(static r => r.IsEnabled))
{
    // ...
}
```

A group must be at least as accessible as the host kit, just like a resource kit.

### Enablement

A kit is built only if it is enabled itself **and** every group it belongs to, plus every ancestor of
those groups, is enabled with a mode that covers the current run. The host kit applies this check before
it builds the kit, in the same place it applies the host kit's own `Mode`. So `IsEnabled` and
`IsResourceEnabled` keep their existing meaning: a kit that a group switches off never reaches
`IsResourceEnabled`.

```text
ExampleHostKit:Groups:Data:IsEnabled=false        # disables postgres, redis and storage
ExampleHostKit:Groups:Platform:Mode=Publish       # every group under Platform is publish-only
ExampleHostKit:Groups:Data:ShowInDashboard=false  # data's kits move up to the platform node
```

### The dashboard

In run mode, at the end of `Configure` (after every `ConfigureResource`, so a resource added there is never
shadowed), each group that has at least one built kit is added as a `ResourceKitGroupResource`. Its
name is the group name, or `<name>-group` (then `-group-2`) when another resource already has it. That
resource has no lifecycle of its own and shows as `Running`, with a folder icon. It is excluded from the
publishing manifest, and in publish mode it is not added at all.

- Each kit is parented to its **primary** group's node. The primary group is the first `[InGroup]`, or
  the one marked `[InGroup<T>(Primary = true)]`. Only one group can be primary (SG0024).
- If the primary group is hidden (`ShowInDashboard = false`), the kit is parented to the nearest
  ancestor group that is shown.
- A child group's node is parented to its parent group's node.
- A kit whose resource already has a parent keeps it, because the dashboard shows one parent per resource.
  The **top** of that parent chain is nested in the group instead. So an integration that builds its own
  tree, such as AspireC4 (`aspirec4` → `aspirec4-likec4` + `aspirec4-er`), lands in the group with its
  shape intact. Two cases are left alone: a chain that ends at another kit's resource (that kit decides
  where its tree goes), and a chain that already ends in a group node. A parent cycle that some other code
  created is left as it is; the walk stops rather than loops.

### No group can contain itself

ResourceKit never lets a group become its own ancestor or member, at any level:

| Level | Guarantee |
| --- | --- |
| Declaration | A group whose parent chain leads back to itself (including `Parent = typeof(Self)` and `ResourceGroup<Self>`) is SG0022. A group can't also be a kit or a group member (SG0031), and a kit can't be in itself, because a kit isn't a group (SG0021). |
| Analysis | The cycle check walks each type's original definition and keeps a set of visited types, so it ends after at most one step per declared group. A self-expanding generic such as `ResourceGroup<G<G<T>>>` on `G<T>` is SG0030. |
| Generation | Any of these errors stops generation. Ordering groups parents-first leaves out anything that can never be ready instead of looping on it. |
| Runtime | `ResourceKitGroup.Parent` is set only in the constructor, before the instance can be anyone's ancestor, so the chain can't loop. `Children`, `Resources` and `GetGroups` are read-only views. Every walk is iterative, and walking down visits each group once, so a deep hierarchy can't exhaust the stack. |
| Dashboard | Group nodes are parented only to group nodes. A kit, or the top of a parent chain a kit already belongs to, is parented only to a group node. The chain is walked with a visited set, so a parent cycle created by other code ends the walk instead of looping. |

### Placing other resources in a group

Some integrations add a resource next to a kit's own, such as DbGate, pgweb or Redis Commander. Those
resources aren't kits, so on their own they sit at the root of the dashboard. `WithResourceGroup` puts one in a group:

```csharp
protected override IResourceBuilder<SqlServerServerResource> BuildResource(IDistributedApplicationBuilder builder)
{
    var sql = builder.AddSqlServer(Name);

    // WithDbGate returns the SQL Server builder; the DbGate builder is passed to the callback.
    sql.WithDbGate(dbGate => dbGate.WithResourceGroup(HostKit.Groups.Persistence));

    return sql;
}
```

- Call it from `BuildResource` or `ConfigureResource`. The resource is placed when the host kit finishes
  `Configure`; calling `WithResourceGroup` after that throws.
- The usual rules apply. A resource that already has a parent keeps it, and the top of its chain is
  nested instead. A resource included in two groups stays in the first. If the group is hidden, the
  resource goes to the nearest shown ancestor group.
- `WithResourceGroup` only affects where the resource appears in the dashboard. Whether it is added at all is up to
  the kit that adds it.
- A group with only included resources still gets a node. A group's own dashboard node can't be included
  (`ArgumentException`).
- To nest the resource under the kit's resource instead, use Aspire directly:
  `sql.WithDbGate(dbGate => dbGate.WithParentRelationship(sql))`.

## Named URLs

```csharp
[ResourceDefinition<Projects.Example_Service>("api")]
[ResourceUrl("Health", "/health", DisplayText = "Health")]
[ResourceUrl("Scalar", "/scalar", Endpoint = "http", DisplayLocation = UrlDisplayLocation.DetailsOnly)]
[EndpointUrl("https", DisplayText = "API")]
sealed partial class ExampleAPIKit { /* ... */ }
```

- **`[ResourceUrl(name, url)]`** adds a link.
  - An absolute URL is shown as it is.
  - A relative URL is resolved against the endpoint named by `Endpoint`. Without one, it uses the
    `https` endpoint, then the `http` endpoint, then the first endpoint.
  - Only **one** URL is added. This differs from Aspire's own `WithUrl("/health")`, which rewrites the
    path of *every* endpoint URL on the resource.
- **`[EndpointUrl(endpoint)]`** changes the URL Aspire already shows for an endpoint: its `DisplayText`
  and `DisplayLocation`.
- A URL's name is the key for its options. An `[EndpointUrl]` is named after its endpoint in PascalCase,
  so `otlp-grpc` becomes `OtlpGrpc`. Names must be unique on a kit or group (SG0026).

Each URL becomes a `ResourceKitUrl` option, so you can switch it off or change it without touching code:

```text
ExampleHostKit:API:Urls:Health:IsEnabled=false
ExampleHostKit:API:Urls:Https:DisplayText=Service
ExampleHostKit:Groups:Platform:Urls:Docs:Url=https://wiki.example.com/platform
```

A group has no endpoints, so it can only show absolute URLs. On a group, `[EndpointUrl]` or a URL with
an `Endpoint` is an error (SG0027), and a relative URL is a warning (SG0028); the URL is skipped at
runtime. The same rules apply to a kit whose resource type has no endpoints, such as a `ParameterResource`.

At runtime, a URL whose endpoint cannot be found is skipped and a warning is logged. The other URLs
still apply.

## Manual host kits

Neither feature depends on the generator. A hand-written host kit uses the runtime types directly:

```csharp
sealed class ExampleHostKit : HostKitBase<ExampleHostKit>
{
    public ExampleHostKit()
    {
        PlatformGroup = new("platform") { Urls = [new() { Url = "https://example.com", DisplayText = "Docs" }] };
        DataGroup = new("data", PlatformGroup);

        Postgres = new(this);
        AddResource(Postgres, DataGroup); // the first group is the primary group
    }
}

sealed class ExampleAPIKit(ExampleHostKit hostKit)
    : ResourceKitBase<ExampleHostKit, ProjectResource>(hostKit, "api")
{
    protected override IEnumerable<ResourceKitUrl> Urls =>
        [new() { Url = "/health", DisplayText = "Health" }, new() { Endpoint = "https", DisplayText = "API" }];
}
```

Any other Aspire builder can use `builder.WithResourceKitUrls(urls)` directly.

## See also

- [Attributes Reference](Attributes-Reference.md)
- [Enablement](Enablement.md)
- [Diagnostics](Diagnostics.md)

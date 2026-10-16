---
name: purview-resourcekit-options
description: >-
  Use when configuring Purview.Aspire.ResourceKit through options - the generated host and resource
  options shape, extending them with custom properties, section-name resolution, disabling a resource
  with IsEnabled, overriding enablement with IsResourceEnabled, and building args or environment
  variables with OptionsHelper.
metadata:
  product: Purview.Aspire.ResourceKit
  repository: purview-dev/aspire-resourcekit
---

# Configuration, enablement and OptionsHelper

ResourceKit generates typed options for the host and for each resource, so names and enablement are
configuration rather than code.

## The generated shape

```csharp
public class ShopHostKitOptions
{
    public const string SectionName = "ShopHostKit";

    public APIKitOptions Api { get; set; } = new();
    public RedisKitOptions Redis { get; set; } = new();
}

public class APIKitOptions
{
    public string Name { get; set; } = "api";
    public bool IsEnabled { get; set; } = true;
}
```

Host options bind to `SectionName` and `ValidateOnStart()` is called; resource options nest under the
generated resource property name. Typical keys: `ShopHostKit:API:Name`, `ShopHostKit:API:IsEnabled`.

## Extending them

Generated options are nested `sealed partial` classes, so you add your own properties on the host kit
or the resource kit:

```csharp
[HostKit]
partial class ShopHostKit
{
    public sealed partial class ShopHostKitOptions
    {
        public bool EnablePreviewResources { get; set; }
    }
}

[ResourceDefinition<ProjectResource>("api")]
sealed partial class APIKit
{
    partial class APIKitOptions
    {
        public string PublishEnvironmentVariableName { get; set; } = "PUBLISH_MARKER";
    }
}
```

Keep custom members `public` with `get; set;` so configuration binding can populate them. Data
annotations such as `[Required]` are honoured by the generated `ValidateOnStart()` registration.

At runtime, host values come from `HostKit.Options` and resource values from `Options`:

```csharp
protected override void ConfigureResource()
{
    if (HostKit.Options.EnablePreviewResources)
        ResourceBuilder.WithEnvironment(Options.PublishEnvironmentVariableName, "true");
}
```

## Section-name resolution

When a section name is not passed explicitly, it resolves in this order:

1. `const string SectionName` on the options type;
2. the type name trimmed by **one** suffix — `Options`, `Settings`, `Configuration`, `Config`;
3. the original type name.

Suffix trimming ignores generic type arguments. `OptionsHelper.SectionNameFor<T>()` (or the `Type`
overload) returns the resolved name.

## `IsEnabled` versus `IsResourceEnabled`

Two toggles, and the interaction is the part people get wrong:

- **`IsEnabled`** — the persisted/configured flag, usually bound from generated options.
- **`IsResourceEnabled(builder)`** — the runtime decision hook. Its default returns `IsEnabled`.

During `Build`:

1. if `IsEnabled` is `true`, `IsResourceEnabled(builder)` is called and its result assigned back to
   `IsEnabled`;
2. if the result is `false`, both `BuildResource(...)` and `ConfigureResource()` are skipped.

Two consequences worth stating plainly:

- **The hook only runs when `IsEnabled` is already `true`.** A kit disabled through options is
  *never* re-enabled by the hook. That is deliberate: configuration is the outer gate.
- **`ResourceBuilder` cannot be accessed while the resource is disabled** — the property guards
  itself and throws `InvalidOperationException`.

Override the hook when enablement depends on runtime state:

```csharp
// Publish-only resources - a Key Vault, a publish marker parameter.
protected override bool IsResourceEnabled(IDistributedApplicationBuilder builder) =>
    builder.ExecutionContext.IsPublishMode;
```

Disabling from configuration needs no code change:

```json
{ "ShopHostKit": { "Redis": { "IsEnabled": false } } }
```

## `OptionsHelper`

Builds configuration arguments or environment variables from strongly typed assignment expressions,
or by flattening a populated options object. Built for integration-test fixtures, scenario toggles
and CLI overrides.

### `Assign`

```csharp
var args = OptionsHelper.Assign<ShopHostKit.ShopHostKitOptions>(
    c => c.API.IsEnabled = false,
    c => c.API.Name = "api-test"
).Build();
// --ShopHostKit:API:IsEnabled=false
// --ShopHostKit:API:Name=api-test
```

**Each action must set exactly one property path.** A block-bodied lambda setting several is
`SG0020`, with a "Split into separate assignments" code fix in Visual Studio:

```csharp
// SG0020
OptionsHelper.Assign<ShopHostKitOptions>(o =>
{
    o.API.IsEnabled = false;
    o.API.Name = "api-test";
});
```

An explicit root section name is accepted too:
`OptionsHelper.Assign<…>("MySection", c => c.API.IsEnabled = false)`.

### `AsEnvironmentVariables`

```csharp
var envVars = OptionsHelper.Assign<ShopHostKit.ShopHostKitOptions>(
    c => c.API.IsEnabled = false
).AsEnvironmentVariables().Build();
// { "ShopHostKit__API__IsEnabled": "false" }
```

### `Environment` — flatten a populated object

```csharp
var envVars = OptionsHelper.Environment(new ShopOptions
{
    Api = new ApiOptions { Name = "api-test", Enabled = true },
    ReplicaNames = ["a", "b"],
    Labels = { ["region"] = "west" },
})
.Override(static o => o.Api.Name = "api-prod")
.Ignore(static o => o.Labels)
.Build();
```

Binder-style keys: `Shop__Api__Name`, `Shop__Api__Enabled`, `Shop__ReplicaNames__0`,
`Shop__ReplicaNames__1`, `Shop__Labels__region`. `Override(...)` replaces values before emission;
`Ignore(...)` drops properties.

The dictionary goes straight to a resource — keys are preserved verbatim, which matters for things
like `Services__ServiceName`:

```csharp
ResourceBuilder.WithEnvironment(OptionsHelper.Environment(Options).Build());
```

### `PathFor`

```csharp
var path = OptionsHelper.PathFor<ShopHostKit.ShopHostKitOptions>(f => f.API.Name);   // "API.Name"
```

### With TUnit.Aspire

```csharp
protected override string[] Args =>
[
    .. base.Args,
    .. OptionsHelper.Assign<ShopHostKit.ShopHostKitOptions>(
        c => c.API.IsEnabled = false,
        c => c.API.Name = "api-test"
    ).Build(),
];
```

## A preference worth keeping

Prefer a resource-level toggle to conditional host code. It keeps the composition declarative, and it
is the difference between a test that flips a config key and one that needs a different AppHost.

## Related skills

`purview-resourcekit-authoring` for the composition model, `purview-resourcekit-testing` for test
hosts, `purview-resourcekit-diagnostics` for `SG0020` and the rest.

# Configuration and options

`Purview.Aspire.ResourceKit` can generate host and resource options types so you can control names and
enablement without changing code.

## Generated options shape

A host options type contains one nested options object per resource.

```csharp
public class ShopHostKitOptions
{
    public APIKitOptions Api { get; set; } = new();
    public RedisKitOptions Redis { get; set; } = new();
}

public class APIKitOptions
{
    public string Name { get; set; } = "api";

    public bool IsEnabled { get; set; } = true;
}

...
```

The generated host options type exposes a `SectionName` constant that is used to bind configuration:

```csharp
public const string SectionName = "ShopHostKit";
```

Generated options are emitted as nested `sealed partial` classes. You can extend both host and resource
option types with custom properties.

## Extend host and resource typed options

### Extend host options

```csharp
[HostKit]
partial class ShopHostKit
{
    public sealed partial class ShopHostKitOptions
    {
        public bool EnablePreviewResources { get; set; }
    }
}
```

### Extend resource options

```csharp
[ResourceDefinition<ProjectResource>("api")]
sealed partial class APIKit
{
    partial class APIKitOptions
    {
        public string PublishEnvironmentVariableName { get; set; } = "PUBLISH_MARKER";
    }
}
```

> Tip: keep custom option members `public` with `get; set;` so configuration binding can populate them.
> Data annotations such as `[Required]` are honored by the generated `ValidateOnStart()` registration.

### Use extended options at runtime

- Access host-level values through `HostKit.Options`.
- Access resource-level values through `Options` in each resource kit.

Example:

```csharp
protected override void ConfigureResource()
{
    if (HostKit.Options.EnablePreviewResources)
    {
        ResourceBuilder.WithEnvironment(Options.PublishEnvironmentVariableName, "true");
    }
}
```

## Common configuration keys

Typical keys (example):

- `ShopHostKit:API:Name`
- `ShopHostKit:API:IsEnabled`

Configuration is bound by the generated extension method:

- Host options bind to `ShopHostKitOptions.SectionName` (for example `ShopHostKit`) and
  `ValidateOnStart()` is called.
- Resource options are nested by generated resource property name.

## Disable a resource

Set `IsEnabled=false` for that resource's options section.

```json
{
  "ShopHostKit": {
    "Redis": {
      "IsEnabled": false
    }
  }
}
```

## `IsEnabled` vs `IsResourceEnabled(...)`

Use both together for flexible control:

- `IsEnabled`: static/configured toggle (usually generated options).
- `IsResourceEnabled(builder)`: runtime decision hook.

At runtime, `Build` only calls `IsResourceEnabled(builder)` when `IsEnabled` is already `true`. If the
hook returns `false`, ResourceKit skips both `BuildResource(...)` and `ConfigureResource()` for that
resource. A kit disabled via `IsEnabled=false` is never re-enabled by the hook.

See [Enablement](Enablement.md) for details.

## OptionsHelper for tests and overrides

For tests and scenario toggles, `OptionsHelper` converts typed assignments into command-line arguments
or environment variables:

```csharp
var args = OptionsHelper.Assign<ShopHostKit.ShopHostKitOptions>(
    c => c.API.IsEnabled = false,
    c => c.API.Name = "api-test"
).Build();
```

See [OptionsHelper](OptionsHelper.md) for the complete API, including `PathFor`, `SectionNameFor`, and
the `SG0020` single-property-path rule.

## Section-name resolution

Generated options types carry a `SectionName` constant (for example `"ShopHostKit"`). When you do not
pass a section name explicitly, `OptionsHelper` resolves it as follows:

1. `const string SectionName` on the options type
2. Type name trimmed by one suffix: `Options`, `Settings`, `Configuration`, `Config`
3. Original type name

You can look up the resolved section name with
[`OptionsHelper.SectionNameFor`](OptionsHelper.md#sectionnamefor).

## Tip

Prefer resource-level toggles over conditional host code. It keeps the composition model declarative
and testable.
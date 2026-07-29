# Configuration and options

`Purview.Aspire.ResourceKit` can generate host and resource options types so you can control names and enablement without changing code.

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

Generated options are emitted as nested `sealed partial` classes. You can extend both host and resource option types with custom properties.

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

> Tip: use `public` + `get; set;` for bindable properties, and give defaults to keep local/test runs resilient.

## Common configuration keys

Typical keys (example):

- `ShopHostKit:API:Name`
- `ShopHostKit:API:IsEnabled`

## Disable a resource

Set `IsEnabled=false` for that resource's options section.

## `IsEnabled` vs `IsResourceEnabled(...)`

Use both together for flexible control:

- `IsEnabled`: static/configured toggle (usually generated options).
- `IsResourceEnabled(builder)`: runtime decision hook.

At runtime, `Build` sets `IsEnabled = IsResourceEnabled(builder)` first. If the result is `false`, ResourceKit skips both `BuildResource(...)` and `ConfigureResource()` for that resource.

Use this hook to react to runtime state, for example environment-specific availability, publish mode, or dynamic configuration checks.

## OptionsHelper for tests and overrides

Use `OptionsHelper` to generate command-line configuration arguments from strongly typed assignments.

```csharp
var args = OptionsHelper.For<ShopHostKitOptions>(
    c => c.API.IsEnabled = false,
    c => c.API.Name = "api-test");
```

Resulting args are in this form:

- `--ShopHostKit:API:IsEnabled=false`
- `--ShopHostKit:API:Name=api-test`

>[!NOTE]
> This can be useful when using [TUnit.Aspire](https://www.nuget.org/packages/TUnit.Aspire/), and passing in Args, e.g.
>
> ```csharp
> protected override string[] Args =>
> [
>    .. base.Args,
>    .. OptionsHelper.For<ShopHostKit.ShopHostKitOptions>(
>      c => c.API.IsEnabled = false,
>      c => c.API.Name = "api-test"
>    ),
>];
> ```

## Section-name resolution order

If you do not pass a section name explicitly, `OptionsHelper` resolves it as follows:

1. `const string SectionName` on the options type
2. Type name trimmed by one suffix: `Options`, `Settings`, `Configuration`, `Config`
3. Original type name

## Tip

Prefer resource-level toggles over conditional host code. It keeps the composition model declarative and testable.

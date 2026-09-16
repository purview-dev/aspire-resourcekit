# Enablement: IsEnabled vs IsResourceEnabled

ResourceKit supports flexible runtime enablement through two related members.

## The two toggles

- `IsEnabled` — the current enablement flag, usually sourced from generated options. This is the
  persisted/configured toggle.
- `IsResourceEnabled(builder)` — a runtime decision hook. Its default implementation returns
  `IsEnabled`, and you override it when enablement should react to runtime conditions.

## How they interact

At runtime, `Build` evaluates enablement *before* resource construction:

1. If `IsEnabled` is `true`, ResourceKit calls `IsResourceEnabled(builder)` and assigns the result
   back to `IsEnabled`.
2. If the resulting `IsEnabled` is `false`, both `BuildResource(...)` and `ConfigureResource()` are
   skipped for that resource.

Two important consequences:

- The hook is only invoked when `IsEnabled` is already `true`. A kit disabled via `IsEnabled=false`
  (for example from options) is **never** re-enabled by the hook.
- `ResourceBuilder` cannot be accessed while the resource is disabled — doing so throws an
  `InvalidOperationException` (the property guards itself).

## When to override `IsResourceEnabled`

Use the hook when enablement depends on runtime state rather than only static options. Common examples
are environment-specific availability, publish mode, or dynamic configuration checks.

```csharp
protected override bool IsResourceEnabled(IDistributedApplicationBuilder builder)
{
    // Example: allow config + environment based behavior.
    var isEnabledInConfig = IsEnabled;
    var isProd = builder.Configuration["ASPNETCORE_ENVIRONMENT"] == "Production";

    return isEnabledInConfig && isProd;
}
```

Publish-only resources are a typical pattern. The example hosts run Key Vault and a publish marker
parameter only when publishing:

```csharp
protected override bool IsResourceEnabled(IDistributedApplicationBuilder builder) =>
    builder.ExecutionContext.IsPublishMode;
```

## Disabling a resource from options

Because `IsEnabled` is bound from generated options, a resource can be disabled without code changes
by setting the options section in configuration:

```json
{
  "ShopHostKit": {
    "Redis": {
      "IsEnabled": false
    }
  }
}
```

See [Configuration and Options](Configuration-and-Options.md) for the generated options shape.

## See also

- [Lifecycle: Build vs Configure](Lifecycle-Build-Configure.md)
- [Configuration and Options](Configuration-and-Options.md)
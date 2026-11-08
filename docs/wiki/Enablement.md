# Enablement: mode, IsEnabled and IsResourceEnabled

ResourceKit decides participation in three ordered steps: the configured **mode**, the configured
`IsEnabled` flag, and an optional runtime hook.

## Run modes

`ResourceKitMode` selects which AppHost execution a host kit or resource kit participates in. It is a
*kit option*, so participation is selected from configuration instead of coded into an
`IsResourceEnabled` override.

| Mode | Participates when | Typical use |
| --- | --- | --- |
| `All` (default) | always | most resources |
| `LocalDevelopment` | run mode **and** the `Development` environment | a documentation site, a diagram site |
| `Run` | run mode, any environment | tooling that must not be published |
| `Publish` | publish mode (`aspire publish` / `azd`) | Azure Application Insights, a container apps environment, Key Vault, publish parameters |

The mode can be declared on the attribute, which sets the default for the generated options:

```csharp
[HostKit]
sealed partial class ShopHostKit;

// Constructor argument.
[ResourceDefinition<AzureKeyVaultResource>("kv", ResourceKitMode.Publish)]
sealed partial class KeyVaultKit;

// Named property.
[ResourceDefinition<ParameterResource>(Mode = ResourceKitMode.Publish)]
sealed partial class PublishMarkerKit;
```

Because it is an option, configuration can override either the host-wide mode or a single resource:

```json
{
  "ShopHostKit": {
    "Mode": "All",
    "KeyVault": {
      "Mode": "Publish"
    },
    "DocsSite": {
      "Mode": "LocalDevelopment"
    }
  }
}
```

- The host `Mode` gates the whole host kit: every resource is disabled before it is built.
- A resource `Mode` gates that resource only.

An excluded kit never runs `BuildResource(...)` or `ConfigureResource()`. Its host property is still
constructed (so `HostKit.KeyVault` is non-null), but `IsEnabled` is `false` and any `ResourceBuilder`
access throws `InvalidOperationException` — always check `IsEnabled` first, exactly as for a kit
disabled through options.

Manual (non-generated) kits express the same behaviour by overriding the `Mode` property:

```csharp
sealed partial class KeyVaultKit(ShopHostKit hostKit)
    : ResourceKitBase<ShopHostKit, AzureKeyVaultResource>(hostKit, "kv")
{
    protected override ResourceKitMode Mode => ResourceKitMode.Publish;
}
```

## The two toggles

- `IsEnabled` — the current enablement flag, usually sourced from generated options. This is the
  persisted/configured toggle, and the only member consumers need to check.
- `IsResourceEnabled(builder)` — a runtime decision hook. Its default implementation returns
  `IsEnabled`, and you override it when enablement should react to runtime conditions that are not a
  mode (for example a feature flag read from configuration).

## How they interact

At runtime, `Build` evaluates enablement *before* resource construction:

1. The configured `Mode` is checked first. If it does not participate in this run, `IsEnabled` becomes
   `false` and the hook is never invoked.
2. If `IsEnabled` is still `true`, ResourceKit calls `IsResourceEnabled(builder)` and assigns the
   result back to `IsEnabled`.
3. If the resulting `IsEnabled` is `false`, both `BuildResource(...)` and `ConfigureResource()` are
   skipped for that resource.

Two important consequences:

- The hook is only invoked when `IsEnabled` is already `true` and the mode participates. A kit
  disabled via `IsEnabled=false` (for example from options) is **never** re-enabled by the hook.
- `ResourceBuilder` cannot be accessed while the resource is disabled — doing so throws an
  `InvalidOperationException` (the property guards itself).

## When to override `IsResourceEnabled`

Use the hook when enablement depends on runtime state that is not an execution mode. Common examples
are environment-specific feature flags or dynamic configuration checks.

```csharp
protected override bool IsResourceEnabled(IDistributedApplicationBuilder builder)
{
    // Example: allow config + feature flag based behavior.
    var isEnabledInConfig = IsEnabled;
    var isFeatureEnabled = builder.Configuration["Features:KeyVault"] == "true";

    return isEnabledInConfig && isFeatureEnabled;
}
```

Publish-only resources no longer need a hook; declare the mode instead:

```csharp
[ResourceDefinition<AzureKeyVaultResource>("kv", ResourceKitMode.Publish)]
sealed partial class KeyVaultKit;
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

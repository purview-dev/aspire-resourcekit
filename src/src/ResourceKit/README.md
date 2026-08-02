# Purview.Aspire.ResourceKit (NuGet package)

This package contains the runtime abstractions and source-generator contracts for building Aspire AppHost resources using strongly typed classes.

Use this README when integrating the **`Purview.Aspire.ResourceKit` NuGet package** into your host project.

## Package goals

- Keep AppHost resource composition explicit and testable.
- Generate repetitive registration/configuration code from attributes.
- Provide strongly typed options for host and resource toggles.

## Public attributes

### `HostKitAttribute` (`[HostKit]`)

Marks the single host kit class per compilation.

```csharp
[HostKit]
sealed partial class ShopHostKit;
```

Optional named arguments:

- `Name` — controls generated naming.
- `ExtensionMethodName` — overrides generated builder extension name.
- `GenerateOptions` — enables/disables generated host options.

### `ResourceDefinitionAttribute` (`[ResourceDefinition]`)

Marks a resource kit class that participates in generation.

```csharp
[ResourceDefinition<ProjectResource>("api", PropertyName = "API")]
partial class APIResourceKit;
```

Options:

- `Name` — logical Aspire resource name.
- `PropertyName` — generated host property name.

### `ResourceDefinition` vs `ResourceDefinition<TResource>`

ResourceKit supports two declaration styles, with different base-type behavior:

#### Generic attribute (recommended)

Use `[ResourceDefinition<TResource>]` when you want the resource type declared directly on the attribute.

```csharp
[ResourceDefinition<ProjectResource>("api")]
partial class APIResourceKit
{
    protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder)
        => builder.AddProject<Projects.Example_Service>(Name);
}
```

- Do **not** declare an explicit base type on the class.
- The generator supplies the host-specific base in generated partial code.

#### Non-generic attribute

Use `[ResourceDefinition]` when you prefer (or need) to specify the resource type through an explicit base type.

```csharp
[ResourceDefinition("api")]
partial class APIResourceKit : ShopHostKitResourceBase<ProjectResource>
{
    protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder)
        => builder.AddProject<Projects.Example_Service>(Name);
}
```

- You **must** declare an explicit valid base type.
- Typically this is the generated host-specific base (`ResourceBase<TResource>`, unlike the `ResourceBase<THostKit, TResource>` that takes the explicit Host Kit as a construction parameter).

Do not mix both attribute styles on the same class.

## Minimal package usage

```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Purview.Aspire.ResourceKit;

[HostKit]
partial class ShopHostKit;

[ResourceDefinition<ProjectResource>("api")]
partial class APIResourceKit
{
    protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder)
        => builder.AddProject<Projects.Example_Service>(Name);
}

var builder = DistributedApplication.CreateBuilder(args);
builder.AddAspireResourceKit();
```

## Generated output (high level)

From your attributed partial classes, the generator emits:

- a host base type for resources (`{Host}ResourceBase<TResource>`),
- host members for each resource definition,
- generated options types (when enabled),
- a builder extension method for registration and lifecycle execution.

## Runtime lifecycle

When the generated extension is invoked, ResourceKit performs:

1. Resource kit instantiation from options.
2. `Build` for each enabled resource.
3. `Configure` for each enabled resource.

This happens before `DistributedApplication.Build()` completes.

### `Build`/`BuildResource` vs `Configure`/`ConfigureResource`

- `Build` calls your `BuildResource(IDistributedApplicationBuilder)` override to **construct** the resource.
- `Configure` calls your `ConfigureResource()` override to **attach** resources to each other after construction.

This separation keeps creation and cross-resource wiring explicit and deterministic.

### How `IsEnabled` and `IsResourceEnabled(...)` interact

- `IsEnabled` is the current enablement flag (usually sourced from generated options).
- During `Build`, ResourceKit evaluates `IsResourceEnabled(builder)` and assigns that result to `IsEnabled`.
- If disabled, both `BuildResource(...)` and `ConfigureResource()` are skipped for that resource.

Override `IsResourceEnabled(builder)` when enablement should react to runtime state rather than only static options.

## Options and configuration

When options are generated:

- Host options root section is the generated host options type name (or configured section).
- Resource options are nested by generated resource property name.
- `IsEnabled` can be used to skip a resource at runtime.

See detailed patterns in [`/docs/configuration.md`](https://github.com/purview-dev/purview-aspire-resourcekit/blob/main/docs/configuration.md).

### Extending generated typed options

Generated host and resource options are `sealed partial` nested classes. You can safely extend them by adding matching partial declarations in your own code.

Host options extension example:

```csharp
[HostKit]
partial class ExampleHostKit
{
    public sealed partial class ExampleHostKitOptions
    {
        public bool EnablePreviewResources { get; set; }
    }
}
```

Resource options extension example:

```csharp
[ResourceDefinition<ProjectResource>("api")]
sealed partial class ExampleAPIKit
{
    partial class ExampleAPIKitOptions
    {
        public string PublishEnvironmentVariableName { get; set; } = "PUBLISH_MARKER";
    }
}
```

Use these values through generated properties:

- `HostKit.Options` for host-level values.
- `Options` for each resource kit instance.

## `OptionsHelper` (tests and CLI args)

`OptionsHelper` converts typed assignment expressions into command-line configuration args:

```csharp
var args = OptionsHelper.For<ExampleHostKit.ExampleHostKitOptions>(
    c => c.Redis.IsEnabled = false,
    c => c.Redis.Name = "dev-redis");
```

Produces values like:

- `--ExampleHostKit:Redis:IsEnabled=false`
- `--ExampleHostKit:Redis:Name=dev-redis`

Useful for integration-test fixtures and scenario toggles.

## Diagnostics

| ID | Severity | Description |
| - | - | - |
| SG0001 | Error | Class must be `partial` |
| SG0002 | Info | No resources defined for the host kit |
| SG0003 | Warning | No host kit defined (but resources exist) |
| SG0004 | Error | Multiple `[HostKit]` classes defined |
| SG0005 | Error | Duplicate resource property name |
| SG0006 | Error | Resource must derive from expected generated base |
| SG0007 | Error | Resource name could not be derived and no `Name` was specified |
| SG0008 | Error | Explicit `PropertyName` is not a valid C# identifier |
| SG0009 | Error | Missing `IServiceCollection` type dependency |
| SG0010 | Error | Missing configuration binder dependency |
| SG0011 | Error | Missing options configuration extensions dependency |
| SG0012 | Error | Non-empty constructors are not supported on attributed classes |
| SG0013 | Error | Mixed `ResourceDefinition` and `ResourceDefinition<TResource>` on the same class is not supported |
| SG0014 | Error | Non-generic `ResourceDefinition` requires an explicit compatible base type |
| SG0015 | Error | Generic `ResourceDefinition<TResource>` must not declare an explicit base type |
| SG0016 | Error | No Aspire resource type could be inferred/found |

For troubleshooting guidance, see [`/docs/diagnostics.md`](https://github.com/purview-dev/purview-aspire-resourcekit/blob/main/docs/diagnostics.md).

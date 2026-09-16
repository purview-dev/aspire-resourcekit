# Generated output

From your `[HostKit]` and `[ResourceDefinition]` declarations, the generator emits all of the
boilerplate that wires your resource kits together. The generated code is marked with
`[CompilerGenerated]` and `[GeneratedCode("HostKitGenerator", ...)]` and is excluded from code coverage.

## What gets generated

- a host resource base class (the concrete host kit deriving from `HostKitBase<THostKit>`),
- resource properties on the host (one per resource definition),
- host + per-resource options (when `GenerateOptions` is enabled),
- a typed `ResourceKitBase<TResource>` base per host kit,
- each resource kit's `sealed partial` skeleton with a constructor and `Options` property,
- an AppHost extension method to build/configure/register the host kit.

## Host kit members

For every `[ResourceDefinition]`, the host kit gets a lazily initialized property. Accessing a
property before `Build` throws, and setting it twice throws:

```csharp
public Purview.Aspire.ResourceKit.Example.AppHost.AppModels.Resources.ExampleAPIKit ExampleAPI
{
    get
    {
        return field ?? throw new global::System.InvalidOperationException(
            "The 'ExampleAPI' resource has not been initialized. Call Build first.");
    }
    private set { ... }
}
```

## Generated host Build/Configure

`Build` instantiates each resource kit from its options, registers it with the base class, invokes the
optional `onBuilt` callback, then calls `base.Build(builder)`:

```csharp
public override void Build(global::Aspire.Hosting.IDistributedApplicationBuilder builder)
{
    // Creating ExampleAPIKit Resource Kit.
    ExampleAPI = new(this, Options.ExampleAPI);

    // Register the discovered app resources with the base class.
    AddResource(ExampleAPI);

    // Now the additional post-build func builder
    onBuilt?.Invoke(this, builder);

    base.Build(builder);
}

public override void Configure()
{
    base.Configure();
    onConfigured?.Invoke(this);
}
```

## Generated resource kit skeleton

Each resource kit derives from the generated `ResourceKitBase<TResource>` and gains a constructor that
accepts the host kit and its options:

```csharp
internal sealed partial class ExampleAPIKit : global::Purview.Aspire.ResourceKit.ResourceKitBase<ProjectResource>
{
    public ExampleAPIKit(ExampleHostKit hostKit, ExampleAPIKitOptions options)
        : base(hostKit, (options ?? throw new global::System.ArgumentNullException(nameof(options))).Name)
    {
        Options = options;
        IsEnabled = options.IsEnabled;
    }

    public ExampleAPIKitOptions Options { get; }
}
```

Your `partial class` supplies `BuildResource(...)` and optionally `ConfigureResource()` and
`IsResourceEnabled(builder)`.

## Generated options

Host options are nested under the host kit and expose a `SectionName` constant plus one nested options
object per resource:

```csharp
public sealed partial class ExampleHostKitOptions
{
    public const string SectionName = "ExampleHostKit";

    public ExampleAPIKitOptions ExampleAPI { get; init; } = new();
}
```

Each resource options type carries `Name` (defaulting to the logical resource name) and `IsEnabled`:

```csharp
public sealed partial class ExampleAPIKitOptions
{
    [global::System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = false)]
    public string Name { get; set; } = "api";

    public bool IsEnabled { get; set; } = true;
}
```

`Name` is decorated with `[Required(AllowEmptyStrings = false)]`; the generated extension method calls
`ValidateOnStart()`.

## Generated extension method

The extension method binds host options from configuration, creates the host kit, and runs the full
lifecycle:

```csharp
public static global::Aspire.Hosting.IDistributedApplicationBuilder AddAspireResourceKit(
    this global::Aspire.Hosting.IDistributedApplicationBuilder builder,
    global::System.Action<ExampleHostKit, global::Aspire.Hosting.IDistributedApplicationBuilder>? onBuilt = null,
    global::System.Action<ExampleHostKit>? onConfigured = null,
    global::System.Action<global::Microsoft.Extensions.Options.OptionsBuilder<ExampleHostKitOptions>>? configureOptions = null
)
{
    var optionsBuilder = builder.Services
        .AddOptions<ExampleHostKitOptions>()
        .BindConfiguration(ExampleHostKitOptions.SectionName);

    configureOptions?.Invoke(optionsBuilder);
    optionsBuilder.ValidateOnStart();

    var hostKitOptions = builder.Configuration
        .GetSection(ExampleHostKitOptions.SectionName)
        .Get<ExampleHostKitOptions>() ?? new();

    ExampleHostKit hostKit = new(onBuilt, onConfigured, hostKitOptions);
    hostKit.Build(builder);
    hostKit.Configure();
    builder.Services.AddSingleton(hostKit);
    return builder;
}
```

The extension method name defaults to `Add<HostKitName>()` (for example `AddAspireResourceKit()` for
`ExampleHostKit`) and can be overridden with `HostKitAttribute.ExtensionMethodName`.

## See also

- [Attributes Reference](Attributes-Reference.md)
- [Lifecycle: Build vs Configure](Lifecycle-Build-Configure.md)
- [Configuration and Options](Configuration-and-Options.md)
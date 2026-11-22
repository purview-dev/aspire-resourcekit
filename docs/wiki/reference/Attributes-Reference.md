# Attributes reference

ResourceKit's public attributes live in the `Purview.Aspire.ResourceKit` namespace. All of them are
generated into the consuming compilation by the bundled source generator, so you do not reference a
separate attribute package. `[HostKit]` and `[ResourceDefinition]` are described here. The grouping and
URL attributes (`[ResourceGroup]`, `[ResourceGroup<TParent>]`, `[InGroup<TGroup>]`, `[ResourceUrl]` and `[EndpointUrl]`) are
described in [Groups and URLs](../concepts/Groups-and-URLs.md).

## `HostKitAttribute` (`[HostKit]`)

Marks the single host kit class per compilation. Exactly one host kit may exist per compilation
(SG0004).

```csharp
[HostKit]
sealed partial class ShopHostKit;
```

Optional named arguments:

| Argument | Type | Default | Purpose |
| --- | --- | --- | --- |
| `Name` | `string?` | `null` | Controls generated naming. |
| `ExtensionMethodName` | `string?` | Derived from host kit name | Overrides the generated builder extension name (for example `AddAspireResourceKit()`). |
| `GenerateOptions` | `bool` | `true` | Enables/disables generated host options. |
| `Mode` | `ResourceKitMode` | `All` | Execution mode in which the host kit, and therefore every resource, participates. |

`Mode` is available as both a named property and a constructor argument, and configuration can override
the generated default:

```csharp
[HostKit(Mode = ResourceKitMode.LocalDevelopment)]
sealed partial class ShopHostKit;

[HostKit(ResourceKitMode.Publish)]
sealed partial class PublishHostKit;
```

See [Enablement](../concepts/Enablement.md) for the mode list and evaluation order.

A host kit's accessibility sets what its generated members can mention. The host kit's options class and
`Groups` accessor are nested inside it, so they take its accessibility; a `[ResourceGroup]` class is emitted
beside it, so it must be declared the same way — `public` under a `public` host kit, `internal` under an
`internal` one (SG0046). See [Groups and URLs](../concepts/Groups-and-URLs.md#resource-groups).

## `ResourceDefinitionAttribute` (`[ResourceDefinition]`)

Marks a resource kit class that participates in generation.

```csharp
[ResourceDefinition<ProjectResource>("api", PropertyName = "API")]
partial class APIResourceKit;
```

Options:

| Argument | Type | Default | Purpose |
| --- | --- | --- | --- |
| `Name` | `string?` | Derived from class name (suffix trimmed) | Logical Aspire resource name. |
| `PropertyName` | `string?` | Derived from class name | Generated host or parent property name. Must be a valid C# identifier (SG0008). |
| `Mode` | `ResourceKitMode` | `All` | Execution mode in which this resource participates. |
| `Parent` | `Type?` | `null` | The resource kit that owns this kit. Use `Parent = typeof(TParentKit)`. |
| `ProjectPath` | `string?` | `null` | The project path for Aspire 17 Project V2 (`AddDotnetProject`). Only used with `DotnetProjectResource` (SG0041). |
| `ResourcePath` | `string?` | `null` | The path for a resource added by path: `AddDotnetProject` or an Aspire JavaScript app (`AddViteApp`, `AddNextJsApp`, `AddNodeApp`, `AddBunApp`, `AddDenoApp`, `AddJavaScriptApp`). Other resource types report SG0043. |
| `AspireResourceType` | generic type argument | `null` | The Aspire `IResource` type (generic style only). |

`Parent` gives the child a typed `Parent` property, a parent-aware constructor, a parent-before-child build
order, and inherited enablement. It is composition ownership, not a general dependency. See
[Parent and child resource kits](../concepts/Hierarchies.md).

`ProjectPath` selects Aspire 17 Project V2. Declare `DotnetProjectResource` as the type argument and
the path. With no `BuildResource` override, ResourceKit generates `AddDotnetProject(builder, Name, path)`.
SG0019 validates an explicit base.

```csharp
[ResourceDefinition<DotnetProjectResource>("api", ProjectPath = "../Api/Api.csproj")]
partial class ApiResourceKit;
```

`ResourcePath` is the general form and works for the same .NET project plus Aspire JavaScript app
resources. When both are set, `ResourcePath` is used and the two must agree (SG0042). A
`ViteAppResource` or `NextJsAppResource` kit gets `AddViteApp`/`AddNextJsApp` generated with the
default `"dev"` script. Node, Bun and Deno apps need a script path, so they keep a hand-written
override:

```csharp
[ResourceDefinition<ViteAppResource>("web", ResourcePath = "../../../web/packages/vite-app")]
partial class WebResourceKit;
```

When you write `BuildResource` yourself, ResourceKit generates nothing. SG0018 then reports an
override that doesn't add the declared path with the method the resource type supports, and SG0044
reports one that only repeats the call ResourceKit would generate. A `ResourcePath` on any other resource type reports
SG0043; a `ProjectPath` on anything but `DotnetProjectResource` keeps reporting SG0041.

The legacy `[ResourceDefinition<Projects.X>]` + `AddProject<T>` form is still supported, including on
Aspire 17 where it reports `CS0618`. See [Project resources](../concepts/Project-Resources.md).

`Mode` accepts a named property or a constructor argument, and configuration can override it:

```csharp
// Publish-only, for example Azure Key Vault or Application Insights.
[ResourceDefinition<AzureKeyVaultResource>("kv", ResourceKitMode.Publish)]
partial class KeyVaultKit;

// Local-development-only, for example a documentation or diagram site.
[ResourceDefinition<ProjectResource>(Mode = ResourceKitMode.LocalDevelopment)]
partial class DocsSiteKit;
```

## `ResourceDefinition` vs `ResourceDefinition<TResource>`

ResourceKit supports two declaration styles with different base-type behavior.

### Generic attribute (recommended)

Use `[ResourceDefinition<TResource>]` when you want the resource type declared directly on the
attribute.

```csharp
[ResourceDefinition<ProjectResource>("api")]
partial class APIResourceKit
{
    protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder)
        => builder.AddProject<Projects.Example_Service>(Name);
}
```

- Do **not** declare an explicit base type on the class (SG0015).
- The generator supplies the host-specific base in generated partial code.

The type argument may be:

- an Aspire resource type implementing `IResource` (used as-is), or
- an `IProjectMetadata` type (the type accepted by `AddProject<TProject>()`), which is mapped to
  `ProjectResource` for the generated base class;
- `DotnetProjectResource` (Aspire 17 Project V2), used as-is, with `ProjectPath` declaring the project
  path that `AddDotnetProject` must add.

Anything else is rejected (SG0016).

### Non-generic attribute

Use `[ResourceDefinition]` when you prefer (or need) to specify the resource type through an explicit
base type.

```csharp
[ResourceDefinition("api")]
partial class APIResourceKit : ResourceKitBase<ProjectResource>
{
    protected override IResourceBuilder<ProjectResource> BuildResource(IDistributedApplicationBuilder builder)
        => builder.AddProject<Projects.Example_Service>(Name);
}
```

- You **must** declare an explicit valid base type (SG0014).
- The base must derive from the generated `ResourceKitBase<TResource>` or the runtime
  `ResourceKitBase<THostKit, TResource>` (SG0006).

### Rules summary

| Situation | Diagnostic | Severity |
| --- | --- | --- |
| Both styles on one class | SG0013 | Error |
| Generic style with explicit base | SG0015 | Error |
| Non-generic style without explicit base | SG0014 | Error |
| Explicit base not derived from a valid Resource Kit base | SG0006 | Error |
| Resource type cannot be inferred/found | SG0016 | Error |

## Constructors

Attributed classes must not declare constructors with parameters or executable constructor bodies
(SG0012). The generator supplies the constructor wiring in generated partial code; the primary
constructor of a resource kit receives the host kit and options. In the manual (non-generated) pattern
you declare it yourself — see [Examples](../Examples.md).

## See also

- [Generated Output](Generated-Output.md) — what the generator emits from these attributes.
- [Groups and URLs](../concepts/Groups-and-URLs.md) — `[ResourceGroup]`, `[InGroup<TGroup>]`, `[ResourceUrl]`, `[EndpointUrl]`.
- [Diagnostics](Diagnostics.md) — full diagnostic reference.
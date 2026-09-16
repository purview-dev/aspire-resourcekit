# Attributes reference

ResourceKit ships two public attributes in the `Purview.Aspire.ResourceKit` namespace. Both are
generated into the consuming compilation by the bundled source generator, so you do not reference a
separate attribute package.

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
| `PropertyName` | `string?` | Derived from class name | Generated host property name. Must be a valid C# identifier (SG0008). |
| `AspireResourceType` | generic type argument | `null` | The Aspire `IResource` type (generic style only). |

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
  `ProjectResource` for the generated base class.

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
you declare it yourself — see [Examples](Examples.md).

## See also

- [Generated Output](Generated-Output.md) — what the generator emits from these attributes.
- [Diagnostics](Diagnostics.md) — full diagnostic reference.
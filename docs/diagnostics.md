# Diagnostics and troubleshooting

ResourceKit source generation reports diagnostics with `SGxxxx` IDs to help you fix model issues quickly.

## Diagnostic reference

| ID | Severity | What it means |
| - | - | - |
| SG0001 | Error | A participating class must be `partial` |
| SG0002 | Info | Host kit exists but no resources were defined |
| SG0003 | Warning | Resources exist but no host kit was defined |
| SG0004 | Error | More than one `[HostKit]` class was found |
| SG0005 | Error | Two resources map to the same generated property name |
| SG0006 | Error | A resource does not derive from the expected generated base |
| SG0007 | Error | Resource name could not be inferred and `Name` was not set |
| SG0008 | Error | Explicit `PropertyName` is not a valid C# identifier |
| SG0009 | Error | Missing `IServiceCollection` dependency |
| SG0010 | Error | Missing configuration binder dependency |
| SG0011 | Error | Missing options configuration extensions dependency |
| SG0012 | Error | Non-empty constructors are not supported on attributed classes |
| SG0013 | Error | Mixed `ResourceDefinition` and `ResourceDefinition<TResource>` usage on one class |
| SG0014 | Error | Non-generic `ResourceDefinition` requires explicit compatible base type |
| SG0015 | Error | Generic `ResourceDefinition<TResource>` cannot declare explicit base type |
| SG0016 | Error | No Aspire resource type could be inferred/found |

## Fast troubleshooting checklist

1. Ensure host/resource classes are marked `partial`.
2. Ensure exactly one `[HostKit]` class exists in the compilation.
3. Ensure resource property names are unique (explicit `PropertyName` can help).
4. Ensure each resource uses a compatible base and definition attribute style.
5. Set explicit `Name` when inference cannot determine resource name.

## Common fixes

### Duplicate generated property names (SG0005)

Use unique `PropertyName` values:

```csharp
[ResourceDefinition<ProjectResource>("api", PropertyName = "Api")]
[ResourceDefinition<ProjectResource>("admin", PropertyName = "AdminApi")]
```

### Invalid `PropertyName` (SG0008)

Use valid C# identifiers only (`Api`, `RedisCache`, `OrderDb`, etc.).

### Multiple host kits (SG0004)

Keep one `[HostKit]` per compilation; split scenarios into separate projects if needed.

### Mixed attribute styles (SG0013)

Use exactly one style per class:

- `[ResourceDefinition("name")]`
- or `[ResourceDefinition<TResource>("name")]`

Do not apply both to the same class.

### Base-type mismatch by style (SG0014 / SG0015)

- If you use non-generic `[ResourceDefinition("name")]`, declare an explicit compatible base.
- If you use generic `[ResourceDefinition<TResource>("name")]`, do not declare an explicit base.

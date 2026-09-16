# Source generator behaviors

The source generator ships alongside the runtime package and is exposed to consuming projects as an
analyzer. This page covers how the generator, analyzers, and code fixes are organized and how they
behave.

## Assembly layout

| Assembly | Contents |
| --- | --- |
| `Purview.Aspire.ResourceKit` | Runtime abstractions (`HostKitBase`, `ResourceKitBase`, `OptionsHelper`, interfaces) |
| `Purview.Aspire.ResourceKit.SourceGeneration` | The `HostKitGenerator` incremental generator, `ResourceKitDiagnosticAnalyzer`, `OptionsHelperAssignAnalyzer`, `ResourceKitDiagnosticSuppressor`, and the shared rule set |
| `Purview.Aspire.ResourceKit.SourceGeneration.CodeFixes` | The `OptionsHelperAssignCodeFixProvider` (separate assembly so the main generator assembly never needs `Microsoft.CodeAnalysis.Workspaces`) |

The generator and code-fix assemblies are packed under `analyzers/dotnet/cs` of the
`Purview.Aspire.ResourceKit` package, so consumers get generation, diagnostics, and IDE code fixes from
a single package reference.

## Incremental generation

`HostKitGenerator` is an `IIncrementalGenerator`. It:

1. Registers the embedded attributes (`HostKitAttribute`, `ResourceDefinitionAttribute`) as
   post-initialization output.
2. Runs a set of incremental value providers over the compilation.
3. Emits a single hint-name file per host kit that contains the host kit, resource kit skeletons,
   options, the typed `ResourceKitBase<TResource>`, and the AppHost extension method.

Generation is skipped when the generator is disabled via settings (`IsSourceGeneratorDisabled`).

## Shared rule set

`ResourceKitRules` is the single source of truth for model rules. Both the
`ResourceKitDiagnosticAnalyzer` (which reports diagnostics in the IDE/build) and the generator (which
uses the same rules to decide whether a kit should be generated) evaluate through this shared helper so
the two never drift apart.

- **Analyzer-owned rules** are reported only by the analyzer; the generator computes them for its
  `ShouldProcess` gating but does not re-report them (avoiding duplicate diagnostics).
- **Generation-blocking rules** (Error severity, SG0001–SG0016) halt generation via the
  `GeneratorResult.ShouldProcess` gate.
- **Execution-only rules** (SG0017–SG0019, warnings) never block generation, so a resource kit with
  incomplete `BuildResource`/`ConfigureResource` wiring is still generated and the host kit output is
  still emitted.

See [Diagnostics](Diagnostics.md) for the full rule reference.

## `OptionsHelperAssignAnalyzer` (SG0020)

A dedicated analyzer reports SG0020 when an `OptionsHelper.Assign` (or chained `IOptionsBuilder.Assign`)
action is a block-bodied lambda that assigns more than one property path. It matches `Assign` calls by
method name, containing namespace, and the `params Action<T>[]` parameter shape — including the
`sectionName` overload. See [OptionsHelper](OptionsHelper.md).

## IDE code fix: "Split into separate assignments"

The `OptionsHelperAssignCodeFixProvider` fixes SG0020 by rewriting a block-bodied lambda that assigns
several properties into one assignment argument per property:

```csharp
// Before (SG0020)
OptionsHelper.Assign<ShopHostKitOptions>(o =>
{
    o.API.IsEnabled = false;
    o.API.Name = "api-test";
});

// After
OptionsHelper.Assign<ShopHostKitOptions>(
    o => o.API.IsEnabled = false,
    o => o.API.Name = "api-test"
);
```

The fix lives in a separate code-fix assembly and uses a stable equivalence key. The generator assembly
never acquires a `Microsoft.CodeAnalysis.Workspaces` dependency; the compiler loads the code-fix
assembly without instantiating its Workspaces-dependent types, and the IDE activates them for fixes.

## `ResourceKitDiagnosticSuppressor` (SGSUP0001)

The suppressor removes `CS8618` ("Non-nullable property must contain a non-null value when exiting
constructor") for **non-nullable** `IResourceBuilder<T>` properties declared on resource kits. These
properties are populated at runtime during `BuildResource`/`ConfigureResource`, so the warning does not
apply. Nullable `IResourceBuilder<T>?` properties are left untouched.

A type qualifies as a resource kit when it carries `[ResourceDefinition]`/`[ResourceDefinition<T>]` or
derives from `Purview.Aspire.ResourceKit.ResourceKitBase<,>` (directly or via the generated
`ResourceKitBase<TResource>`).

## Incremental caching

The generator uses incremental value providers and equatable models so that unrelated edits do not
re-trigger generation. The `GeneratorCachingTests` integration suite verifies that generation is
cached across incremental runs and only recomputed when inputs change.

## See also

- [Generated Output](Generated-Output.md)
- [Diagnostics](Diagnostics.md)
- [Contributing](Contributing.md)
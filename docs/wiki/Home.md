# Purview Aspire ResourceKit Wiki

This wiki is the project documentation hub for `Purview.Aspire.ResourceKit`, a
source-generator-powered framework for structuring .NET Aspire AppHost resource composition as
strongly typed, test-friendly classes.

If your AppHost is getting bigger, ResourceKit keeps resource setup maintainable and discoverable by
moving composition into focused resource classes and generating the plumbing for you.

## Start here

- [Getting Started](Getting-Started.md)
- [Attributes Reference](Attributes-Reference.md)
- [Generated Output](Generated-Output.md)
- [Lifecycle: Build vs Configure](Lifecycle-Build-Configure.md)
- [Enablement: IsEnabled vs IsResourceEnabled](Enablement.md)
- [Configuration and Options](Configuration-and-Options.md)
- [OptionsHelper](OptionsHelper.md)
- [Project Resources](Project-Resources.md)
- [Diagnostics](Diagnostics.md)
- [Testing with ResourceKit](Testing-with-ResourceKit.md)
- [Examples: Generated vs Manual](Examples.md)
- [Bundled Agent Skills](Agent-Skills.md)
- [Source Generator Behaviors](Source-Generator-Behaviors.md)
- [Contributing](Contributing.md)
- [Release Flow](Release-Flow.md)

## Why teams use it

- **Cleaner AppHost code** with resource logic split into dedicated classes.
- **Strong typing + IntelliSense** instead of stringly-typed setup.
- **Generated wiring** for host/resource options and registration.
- **Predictable lifecycle** (`Build` then `Configure`) for inter-resource dependencies.
- **Testability** with options overrides and isolated resource composition.

## Feature highlights

- **`[HostKit]`** marks the single host kit class per compilation and generates the host wiring and
  an AppHost extension method (for example `builder.AddAspireResourceKit()`).
- **`[ResourceDefinition]`** marks a resource kit class; generic and non-generic styles are both
  supported.
- **Generated typed options** are emitted as nested `sealed partial` classes you can extend with your
  own settings.
- **Build vs Configure** gives a deterministic lifecycle: build each resource, then connect the
  created resources.
- **`IsEnabled` + `IsResourceEnabled(builder)`** lets enablement react to runtime state (environment,
  publish mode, dynamic configuration).
- **`OptionsHelper`** converts typed assignment expressions into command-line args or environment
  variables for tests and scenario toggles.
- **Diagnostics** (`SG0001`–`SG0020`) report model problems early, with an IDE code fix for
  `OptionsHelper.Assign` multi-assignment actions.
- **Bundled agent skills** auto-install into consuming repositories unless opted out.
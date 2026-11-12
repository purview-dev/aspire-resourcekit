# .NET 11 toolchain and feature adoption

This page records what the repository takes from the .NET 11 SDK and the .NET 11 runtime, and what it
deliberately does not. The short version: **.NET 11 is a shipped leg, not a new floor**, and every
consumer-facing dependency is still declared at the .NET 10 servicing band.

## Toolchain

| Piece | Version | Why |
| --- | --- | --- |
| .NET SDK | `11.0.100` (GA) | C# 15, .NET 11 runtime/tooling; `rollForward: latestFeature` keeps a feature band, not a major |
| `Purview.BuildSdk` | `2.0.1` | Curated TFM sets, C# 15 defaults, the `net11.0+` memory-safety rules, and the bootstrapped `Directory.Build.rsp` |
| `Aspire.AppHost.Sdk` | `13.6.5` | Includes the `Aspire.Hosting` security fix that `13.6.4` lacked |
| `Purview.SourceGeneratorFramework` | `2.0.0` | Generator/runtime split; analyzer stays on `netstandard2.0` |
| `TUnit` | `1.83.0` | Has `net10.0` and `net11.0` assets, so each test leg runs a first-class build |
| `Microsoft.SourceLink.GitHub` | `11.0.100` | Build-time only; matches the SDK band |

`Purview.BuildSdk` 2.x resolves the build from its `Current` set, `net10.0;net11.0`. The runtime
package therefore ships `lib/net10.0` and `lib/net11.0`. The two Roslyn components remain
`netstandard2.0`, because they load in the compiler/IDE host rather than in a consumer process.

## What is enabled

| Area | Feature | Where it applies |
| --- | --- | --- |
| Language | C# 15 (`LangVersion=latest`) | Both legs. Syntax that needs .NET 11 runtime types (unions, `closed`) is available only to the `net11.0` leg |
| Runtime | Runtime Async (`runtime-async=on`) | `net11.0` targets. Opt-in in BuildSdk 2.0.1 (2.0.0 defaulted it on), so `src/Directory.Build.props` sets `PurviewRuntimeAsync=true`. Production code is synchronous, so this is exercised by the net11 test leg, not by a changed library contract. The sample AppHosts stay on the net10.0 floor because `Aspire.AppHost.Sdk` cannot multi-target |
| Runtime | `MemorySafetyRules=2` | `net11.0` targets, the BuildSdk 2.0.1 default. The library does not use `unsafe`, so this is a safety net rather than a code change |
| Build | Multithreaded MSBuild (`-mt`) and the MSBuild server | Repository-wide via the SDK's bootstrapped response file; no project setting |
| Analysis | `AnalysisLevel=latest`, `AnalysisMode=All`, `EnforceCodeStyleInBuild` | Both legs, via the SDK |
| Supply chain | `NuGetAudit=true`, `NuGetAuditMode=all` | Both legs, via the SDK |
| Testing | Per-TFM test legs | `dotnet test` runs the same suite on `net10.0` and `net11.0` |
| Testing | `--timeout`, `--maximum-failed-tests`, `--results-directory-layout per-module` | `Justfile` test recipes; a hung fixture or a cascading failure stops the run, and each module/TFM keeps its own report directory |
| Packaging | Package validation against the published `1.1.0` baseline | `src/Directory.Build.props`; a removed or changed public API fails `pack` |
| Packaging | Two runtime assets, one analyzer asset | `lib/net10.0` + `lib/net11.0`; the generator under `analyzers/dotnet/cs` serves both |
| Debug | Portable PDBs, SourceLink, embedded untracked sources, `.snupkg` | Supplied by `Purview.BuildSdk`; unchanged by this upgrade |

`MemorySafetyRules` and C# 15's runtime-dependent features are **not** enabled by hand: `Purview.BuildSdk`
2.0.1 owns those switches for `net11.0+`. Runtime Async is the deliberate exception — 2.0.1 made it
opt-in after 2.0.0 defaulted it on, so `src/Directory.Build.props` sets `PurviewRuntimeAsync=true` and
`BuildSdkAdoptionTests` fails if that setting disappears. A patch upgrade cannot silently turn the
feature off again.

## Deliberately not adopted

| Feature | Why not |
| --- | --- |
| C# 15 pointer relaxations / `unsafe` expressions | The repository has no `unsafe` code and keeps `AllowUnsafeBlocks` at its compiler default; enabling a preview memory-safety model would add review surface without a workload |
| `DOTNET_EnableCrashReport=1` | Useful for a self-hosted service that crashes natively, but test runs would leave crash artifacts on every failing leg; portable PDBs and SourceLink already cover managed diagnostics |
| File-based apps, `#:include`, `dotnet run -e` | Repository workflows are solution/project based; these are developer convenience features with no CI or package contract |
| Container publishing, multi-arch builds, `SOURCE_DATE_EPOCH` | The repository publishes a NuGet package, not a container image |
| Mobile test targets / device selection | Not applicable |
| `PublishReadyToRun` / Native AOT | The package is a library and an analyzer; the consumer owns deployment, and R2R/AOT would change the shipped asset contract |
| Union types / `closed` hierarchies | Assessed below; no suitable API in this library |

## Dependency floor

Every `Microsoft.Extensions.*` reference stays in the `10.x` band (`10.0.15` for the runtime band,
`10.11.0` for the extensions libraries), even for the `net11.0` asset. This is the lowest common
servicing band: a .NET 10 consumer inherits it, and a .NET 11 consumer is perfectly able to resolve the
`net10.0` assets of those packages. Raising them to `11.0.0` because the repository builds with the
.NET 11 SDK would force an unnecessary upgrade on every .NET 10 consumer.

`PackageToolchainTests` fails the build if a `Microsoft.Extensions.*` version drifts out of the floor
band, or if `net8.0`/`net9.0` is reintroduced anywhere in the repository's target-framework
declarations.

## Union types and `closed` hierarchies: re-assessed

Both were evaluated for the .NET 11 leg and neither is used in the shipped library.

- **Union types are not required.** The only closed set of alternative values in the runtime is the
  set of scalar option values that arrive through `PropertyInfo.GetValue`, and reflection has already
  boxed those values before any union could receive them. A custom union would add a tag and a field
  per case without removing that allocation, so it would be slower and larger than the current
  conversion. If a future `net11.0`-only API does need a union, it should implement the non-boxing
  access pattern (`HasValue` plus a `TryGetValue` overload per case) rather than leaning on the boxed
  `Value` property.
- **`closed` is not appropriate.** `HostKitBase<THostKit>` and `ResourceKitBase<THostKit, TResource>`
  are public extension points: a consumer's generated kits derive from them. Marking either `closed`
  would prevent exactly the inheritance the product exists to support, and would be a breaking change.
  Types that are not extension points are already `sealed`; `sealed` is stronger than `closed` and is
  the correct tool there.
- **The non-boxing win that was taken** is `OptionsHelper.PathFor<TOptions, TValue>(...)`. The existing
  `object?`-returning selector forces the compiler to box a value-type member into the expression tree.
  The generic overload keeps the value in its natural type, and `Ignore` now resolves selectors to
  paths once at call time rather than re-walking expressions at `Build()`.

## See also

- [Versioning and compatibility](Versioning.md#supported-target-frameworks)
- [Release flow](Release-Flow.md)
- [Testing with ResourceKit](Testing-with-ResourceKit.md)

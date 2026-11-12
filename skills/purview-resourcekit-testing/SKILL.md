---
name: purview-resourcekit-testing
description: >-
  Use when testing a Purview.Aspire.ResourceKit AppHost - the TUnit.Aspire fixture pattern,
  integration-testing a running AppHost, overriding generated options from a test with OptionsHelper
  or raw args, unit-testing the manual kit pattern, and the treenode filter for running the tiers.
metadata:
  product: Purview.Aspire.ResourceKit
  repository: purview-dev/aspire-resourcekit
---

# Testing with ResourceKit

ResourceKit is test-friendly by construction: composition is split into focused classes, and the
generated options give a typed way to override names and enablement from a test without a second
AppHost.

## Test tiers

ResourceKit requires **.NET 10** (the floor; the package also ships a `net11.0` asset). This repository uses
[TUnit](https://thomhurst.github.io/TUnit/) with [TUnit.Aspire](https://www.nuget.org/packages/TUnit.Aspire/),
under `src/tests`:

| Project | Covers |
| --- | --- |
| `ResourceKit.UnitTests` | runtime behaviour of the kit base types, run modes and `OptionsHelper` |
| `ResourceKit.IntegrationTests` | starts the example AppHosts and asserts against real resources |
| `SourceGeneration.UnitTests` / `SourceGeneration.IntegrationTests` | generator output, diagnostics, suppression, code fixes, caching |
| `Repository.UnitTests` | the repository itself: authored skills, the architecture policy and its `Purview.BuildSdk` opt-ins |

Reports land in `TestResults/`. The project **name** sets the tier (`*.UnitTests` → `Category=Unit`);
nothing is tagged per test.

## Integration-testing an AppHost

Declare a fixture for the AppHost under test; `TUnit.Aspire` wires its start/stop lifecycle around
the fixture:

```csharp
using TUnit.Aspire;

namespace Purview.Aspire.ResourceKit.Fixtures;

public sealed class ExampleAppHostFixture<TAppHost> : AspireFixture<TAppHost>
    where TAppHost : class;
```

```csharp
using Projects;

[ClassDataSource<ExampleAppHostFixture<Example_AppHost>>(Shared = SharedType.PerTestSession)]
public sealed class ExampleAppHostIntegrationTests(ExampleAppHostFixture<Example_AppHost> fixture)
{
    [Test]
    public async Task AppHost_WhenServicesStarted_APIIsHealthy(CancellationToken cancellationToken)
    {
        // Arrange
        // The fixture is a running AppHost, started once for the test session.

        // Act
        var client = fixture.CreateHttpClient("api");
        var response = await client.GetAsync(new Uri("/health", UriKind.Relative), cancellationToken);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
```

`SharedType.PerTestSession` matters: starting an AppHost per test is slow enough to change how you
write the suite.

For a hierarchy, assert the parent and child Aspire resources directly instead of only the generated
property shape: build the real `DistributedApplication` model, check the child resource is created through
`Parent.ResourceBuilder`, and check that a disabled parent removes the whole subtree. `ResourceKit.UnitTests`
does this for PostgreSQL, VNET/subnets, storage and Service Bus without provisioning infrastructure. The
child's options path is nested (`HostKit:Parent:Child:IsEnabled`).

## Overriding options from a test

The generated extension binds host options from configuration, so a fixture can pass command-line
arguments to toggle resources.

Raw strings work:

```csharp
public sealed class CustomOptionsExampleAppHostFixture : AspireFixture<Projects.Example_AppHost>
{
    public const string AzureStorageName = "custom-options-azure-storage-example";

    protected override string[] Args =>
    [
        "--ExampleHostKit:Redis:IsEnabled=false",
        $"--ExampleHostKit:AzureStorage:Name={AzureStorageName}",
    ];
}
```

But prefer `OptionsHelper.Assign<TOptions>(...)` — typed, and refactor-safe:

```csharp
protected override string[] Args =>
[
    .. base.Args,
    .. OptionsHelper.Assign<ExampleHostKit.ExampleHostKitOptions>(
        c => c.Redis.IsEnabled = false,
        c => c.AzureStorage.Name = "custom-options-azure-storage-example"
    ).Build(),
];
```

Each `Assign` action must set exactly one property path (`SG0020`). The
`purview-resourcekit-options` skill covers `Assign`, `AsEnvironmentVariables`, `Environment`,
`PathFor` and `SectionNameFor`.

The same route flips a resource's run mode, which is how a test exercises publish-only or
local-development-only kits without a second AppHost:

```csharp
.. OptionsHelper.Assign<ExampleHostKit.ExampleHostKitOptions>(
    c => c.KeyVault.Mode = ResourceKitMode.LocalDevelopment
).Build(),
```

Assert that the override actually took effect, rather than that the host started:

```csharp
[Test]
public async Task AppHost_WithCustomOptions_IsPassedToTheHostKit(CancellationToken cancellationToken)
{
    // Arrange
    // The fixture starts the AppHost with the options above as command-line arguments.

    // Act
    await fixture.ConnectionStringIsUnavailableAsync(Platform.ResourceKits.Redis, cancellationToken);

    // Assert
    await Assert
        .That(fixture.GetResourceSnapshot(CustomOptionsExampleAppHostFixture.AzureStorageName))
        .IsNotNull()
        .Because("The custom Azure Storage name should be passed to the host kit.");
}
```

If you already have a populated options object, `OptionsHelper.Environment(...)` flattens it into
configuration-style environment variables, with `Override(...)` and `Ignore(...)` to adjust before
emission.

## Unit-testing the manual pattern

The manual host pattern composes `ResourceKitBase<THostKit, TResource>` without generation, which
makes the lifecycle directly unit-testable: instantiate a kit, call `Build(builder)` then
`Configure()`, and assert on `ResourceBuilder` and the option-driven behaviour. Use it when you want
to test lifecycle semantics without starting anything.

## Running them

Unit tier only — integration tests need Docker/Testcontainers:

```bash
just test "/*/*/*/*[Category=Unit]"
```

Everything, locally:

```bash
just test
```

Filtering uses the TUnit **tree-node** filter (`--treenode-filter`), not `--filter`: this repository
runs on Microsoft.Testing.Platform, where `dotnet test --filter` reports "Zero tests ran".

## Related skills

`purview-resourcekit-options` for `OptionsHelper` and enablement, and
`purview-resourcekit-authoring` for the lifecycle these tests exercise.

# Testing with ResourceKit

ResourceKit is designed to be test-friendly: resource composition is split into focused classes, and
generated options give you a typed way to override names and enablement from tests.

## Test project layout

The repository uses [TUnit](https://thomhurst.github.io/TUnit/) with the
[TUnit.Aspire](https://www.nuget.org/packages/TUnit.Aspire/) integration. Test projects live under
`src/tests`:

- `ResourceKit.UnitTests` — runtime behavior of the kit base types and `OptionsHelper`.
- `ResourceKit.IntegrationTests` — starts the example AppHosts and asserts against real resources.
- `SourceGeneration.UnitTests` / `SourceGeneration.IntegrationTests` — generator output, diagnostics,
  suppression, code fixes, and caching.

Reports are written to `TestResults/`.

## Integration-testing an AppHost

Create an `AspireFixture<TAppHost>` for the AppHost under test:

```csharp
using TUnit.Aspire;

namespace Purview.Aspire.ResourceKit.Fixtures;

public sealed class ExampleAppHostFixture<TAppHost> : AspireFixture<TAppHost>
	where TAppHost : class;
```

Then drive it from a TUnit test class:

```csharp
using Projects;

[ClassDataSource<ExampleAppHostFixture<Example_AppHost>>(Shared = SharedType.PerTestSession)]
public sealed class ExampleAppHostIntegrationTests(ExampleAppHostFixture<Example_AppHost> fixture)
{
	[Test]
	public async Task AppHost_WhenServicesStarted_APIIsHealthy(CancellationToken cancellationToken)
	{
		var client = fixture.CreateHttpClient("api");
		var response = await client.GetAsync(new Uri("/health", UriKind.Relative), cancellationToken);

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
	}
}
```

`TUnit.Aspire` wires the AppHost lifecycle (start/stop) around the fixture. The example API exposes a
`/health` endpoint for this purpose.

## Overriding options from tests

The generated extension method binds host options from configuration, so you can pass command-line
arguments through the fixture to toggle resources. `OptionsHelper` generates these arguments from
strongly typed assignments:

```csharp
using TUnit.Aspire;

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

The test then asserts the options took effect — Redis is disabled (no connection string), and Azure
Storage is registered under the custom name:

```csharp
[Test]
public async Task AppHost_WithCustomOptions_IsPassedToTheHostKit(CancellationToken cancellationToken)
{
	await Helpers.ConnectionStringIsUnavailableAsync(fixture, "redis", cancellationToken);

	await Assert
		.That(fixture.GetResourceSnapshot(CustomOptionsExampleAppHostFixture.AzureStorageName))
		.IsNotNull()
		.Because("The custom Azure Storage name should be passed to the host kit.");
}
```

Prefer `OptionsHelper.Assign<TOptions>(...)` for typed, refactor-safe args:

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

See [OptionsHelper](OptionsHelper.md) for `Assign`, `AsEnvironmentVariables`, and the `SG0020`
single-property-path rule.

If you already have a populated options object, use `OptionsHelper.Environment(...)` to flatten it into
configuration-style environment variables and selectively override or ignore members before emission.

## Unit-testing the manual pattern

The manual host pattern (see [Examples](Examples.md)) composes `ResourceKitBase<THostKit, TResource>`
without generation, which makes the lifecycle directly unit-testable: instantiate a kit, call
`Build(builder)`/`Configure()`, and assert on `ResourceBuilder` and option-driven behavior.

## Running tests

Unit tests are filtered with the TUnit tree-node filter:

```bash
just test "/*/*/*/*[Category=Unit]"
```

Integration tests (which require Docker/Testcontainers) run locally:

```bash
just test
```

See [Contributing](Contributing.md) and [Release Flow](Release-Flow.md) for the CI filter and local
workflow.

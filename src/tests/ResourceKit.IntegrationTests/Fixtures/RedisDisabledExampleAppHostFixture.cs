using Projects;
using Purview.Aspire.ResourceKit.Example;
using Purview.Aspire.ResourceKit.Example.AppHost.AppModels;
using TUnit.Aspire;

namespace Purview.Aspire.ResourceKit.Fixtures;

public sealed class RedisDisabledExampleAppHostFixture : AspireFixture<Example_AppHost>
{
	protected override string[] Args =>
		[
			.. base.Args,
			$"--{ExampleHostKit.ExampleHostKitOptions.SectionName}:{nameof(ExampleHostKit.ExampleHostKitOptions.DisabledResources)}:0={Platform.ResourceKits.Redis}",
		];
}

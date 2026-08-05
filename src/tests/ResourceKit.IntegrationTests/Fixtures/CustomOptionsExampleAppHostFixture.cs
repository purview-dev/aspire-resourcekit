using Projects;
using Purview.Aspire.ResourceKit.Example.AppHost.AppModels;
using TUnit.Aspire;

namespace Purview.Aspire.ResourceKit.Fixtures;

public sealed class CustomOptionsExampleAppHostFixture : AspireFixture<Example_AppHost>
{
	public const string AzureStorageName = "custom-options-azure-storage-example";

	protected override string[] Args =>
		[
			.. base.Args,
			.. OptionsHelper
				.ForSet<ExampleHostKit.ExampleHostKitOptions>(
					c => c.Redis.IsEnabled = false,
					c => c.AzureStorage.Name = AzureStorageName
				)
				.Build(),
		];
}

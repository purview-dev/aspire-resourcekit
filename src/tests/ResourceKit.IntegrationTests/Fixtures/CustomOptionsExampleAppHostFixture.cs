using TUnit.Aspire;

namespace Purview.Aspire.ResourceKit.Fixtures;

public sealed class CustomOptionsExampleAppHostFixture : AspireFixture<Projects.Example_AppHost>
{
	public const string AzureStorageName = "custom-options-azure-storage-example";

	protected override string[] Args =>
		["--ExampleHostKit:Redis:IsEnabled=false", $"--ExampleHostKit:AzureStorage:Name={AzureStorageName}"];
}

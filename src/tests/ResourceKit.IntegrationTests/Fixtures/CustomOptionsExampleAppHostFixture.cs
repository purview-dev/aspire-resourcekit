using TUnit.Aspire;

namespace Purview.Aspire.ResourceKit.Fixtures;

public sealed class CustomOptionsExampleAppHostFixture : AspireFixture<Projects.Example_AppHost>
{
	public const string AzureStorageName = "custom-options-azure-storage-example";
	public const string RedisAliasName = "custom-options-redis-alias";

	protected override string[] Args =>
		[
			"--ExampleHostKit:Redis:IsEnabled=false",
			$"--ExampleHostKit:AzureStorage:Name={AzureStorageName}",
			$"--ExampleHostKit:Redis:Labels:Alias={RedisAliasName}",
		];
}

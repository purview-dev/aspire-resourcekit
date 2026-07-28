using Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels.Resources;

namespace Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels;

sealed class ExampleHostKit : HostKitBase<ExampleHostKit>
{
	public AzureStorageKit AzureStorage { get; init; }

	public ExampleAPIKit ExampleAPI { get; init; }

	public KeyVaultKit KeyVault { get; init; }

	public PublishMarkerKit PublishMarker { get; init; }

	public RedisKit Redis { get; init; }

	public PostgresKit Postgres { get; init; }

	public ExampleHostKit()
	{
		AzureStorage = new(this);
		ExampleAPI = new(this);
		KeyVault = new(this);
		PublishMarker = new(this);
		Redis = new(this);
		Postgres = new(this);

		Resources = [AzureStorage, ExampleAPI, KeyVault, PublishMarker, Redis, Postgres];
	}
}

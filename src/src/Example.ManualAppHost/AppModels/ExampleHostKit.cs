using Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels.Resources;

namespace Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels;

sealed class ExampleHostKit : HostKitBase<ExampleHostKit>
{
	public AzureStorageAppResource AzureStorage { get; } = new();

	public ExampleAPIAppResource ExampleAPI { get; } = new();

	public KeyVaultAppResource KeyVault { get; } = new();

	public PublishMarkerAppResource PublishMarker { get; } = new();

	public RedisAppResource Redis { get; } = new();

	public PostgresAppResource Postgres { get; } = new();

	public ExampleHostKit()
	{
		Resources = [AzureStorage, ExampleAPI, KeyVault, PublishMarker, Redis, Postgres];
	}
}

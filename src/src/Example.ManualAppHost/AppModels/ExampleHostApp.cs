using Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels.Resources;

namespace Purview.Aspire.ResourceKit.Example.ManualAppHost.AppModels;

sealed class ExampleHostApp : HostAppBase<ExampleHostApp>
{
	public AzureStorageAppResource AzureStorage { get; } = new();

	public ExampleAPIAppResource ExampleAPI { get; } = new();

	public KeyVaultAppResource KeyVault { get; } = new();

	public PublishMarkerAppResource PublishMarker { get; } = new();

	public RedisAppResource Redis { get; } = new();

	public SqlServerAppResource SqlServer { get; } = new();

	public ExampleHostApp()
	{
		Resources = [AzureStorage, ExampleAPI, KeyVault, PublishMarker, Redis, SqlServer];
	}
}

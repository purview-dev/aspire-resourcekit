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

	public ResourceKitGroup<ExampleHostKit> PlatformGroup { get; init; }

	public ResourceKitGroup<ExampleHostKit> DataGroup { get; init; }

	public ResourceKitGroup<ExampleHostKit> ServicesGroup { get; init; }

	public ExampleHostKit()
	{
		PlatformGroup = new(Platform.ResourceGroups.Platform)
		{
			Urls = [new() { Url = Platform.ResourceGroups.DocsUrl, DisplayText = "ResourceKit docs" }],
		};
		DataGroup = new(Platform.ResourceGroups.Data, PlatformGroup);
		ServicesGroup = new(Platform.ResourceGroups.Services, PlatformGroup);

		AzureStorage = new(this);
		ExampleAPI = new(this);
		KeyVault = new(this);
		PublishMarker = new(this);
		Redis = new(this);
		Postgres = new(this);

		AddResource(AzureStorage, DataGroup);
		AddResource(ExampleAPI, ServicesGroup);
		AddResource(KeyVault);
		AddResource(PublishMarker);
		AddResource(Redis, DataGroup);
		AddResource(Postgres, DataGroup);
	}
}

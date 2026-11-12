namespace Purview.Aspire.ResourceKit.Example;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible")]
public static class Platform
{
	public static class ResourceKits
	{
		public const string Postgres = "postgres";
		public const string PostgresDb = "db";

		public const string AzureStorage = "azure-storage";
		public const string AzureStorageBlob = "blob";

		public const string Redis = "redis";

		public const string Vnet = "vnet";
		public const string SubnetWeb = "snet-web";
		public const string SubnetData = "snet-data";
		public const string SubnetPrivateEndpoint = "snet-private-endpoint";

		public const string KeyVault = "kv";

		public const string API = "api";

		public const string PublishMarker = "publish-marker";
	}

	public static class ResourceGroups
	{
		public const string Platform = "platform";

		public const string Data = "data";

		public const string Services = "services";

		public const string DocsUrl = "https://purview.dev/projects/aspire-resourcekit/";
	}

	public static class EndpointsDefinitions
	{
		public const string Health = "/health";
		public const string Aliveness = "/alive";
	}
}

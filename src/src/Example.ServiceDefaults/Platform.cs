namespace Purview.Aspire.ResourceKit.Example;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Design",
	"CA1034:Nested types should not be visible"
)]
public static class Platform
{
	public static class ResourceKits
	{
		public const string Postgres = "postgres";
		public const string PostgresDb = "db";

		public const string AzureStorage = "azure-storage";
		public const string AzureStorageBlob = "blob";

		public const string Redis = "redis";

		public const string KeyVault = "kv";

		public const string API = "api";

		public const string PublishMarker = "publish-marker";
	}

	public static class EndpointsDefinitions
	{
		public const string Health = "/health";
		public const string Aliveness = "/alive";
	}
}

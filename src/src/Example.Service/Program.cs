using Purview.Aspire.ResourceKit.Example;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureBlobServiceClient(Platform.ResourceKits.AzureStorageBlob);
builder.AddAzureNpgsqlDataSource(Platform.ResourceKits.PostgresDb);
builder.AddRedisClient(Platform.ResourceKits.Redis);

if (!builder.Environment.IsDevelopment())
	builder.AddAzureKeyVaultKeyClient(Platform.ResourceKits.KeyVault);

var app = builder.Build();

app.MapDefaultEndpoints();

await app.RunAsync();

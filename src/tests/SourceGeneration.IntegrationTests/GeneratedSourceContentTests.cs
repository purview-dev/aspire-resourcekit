namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Asserts the structure and content of the host app source emitted by
/// <see cref="HostAppGenerator"/> for a range of host app / app resource configurations.
/// </summary>
public class GeneratedSourceContentTests : IncrementalSourceGeneratorTestBase<HostAppGenerator>
{
	[Test]
	public async Task Generate_GivenHostAppWithSingleResource_GeneratesExpectedHostAppSource(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[HostApp]
	public partial class TestingHostApp { }

	[ResourceDefinition(""redis"")]
	public partial class RedisAppResource : TestingHostAppResourceBase<object> { }
}
";

		var (result, _) = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();
		await Assert.That(ExcludeGeneratedAttributes(result)).Count().IsEqualTo(1);

		var generated = GetGeneratedSource(result);
		await Assert.That(generated).Contains("partial class RedisAppResource");
		await Assert.That(generated).Contains("abstract class TestingHostAppResourceBase<TResource>");
		await Assert
			.That(generated)
			.Contains("global::Purview.Aspire.ResourceKit.ResourceKitBase<global::Testing.TestingHostApp, TResource>");
		await Assert.That(generated).Contains("abstract partial class TestingHostAppResourceOptionsBase");
		await Assert
			.That(generated)
			.Contains("sealed partial class RedisAppResourceOptions : global::Testing.TestingHostAppResourceOptionsBase");
		await Assert.That(generated).Contains("public const string SectionName = \"Redis\";");
		await Assert.That(generated).Contains("public RedisAppResourceOptions() => Name = \"redis\";");
		await Assert
			.That(generated)
			.Contains("protected TestingHostAppResourceBase(TestingHostAppResourceOptionsBase options)");
		await Assert
			.That(generated)
			.Contains(
				"partial class TestingHostApp(TestingHostAppOptions hostAppOptions) : global::Purview.Aspire.ResourceKit.HostAppBase<global::Testing.TestingHostApp>"
			);
		await Assert.That(generated).Contains("public global::Testing.RedisAppResource Redis");
		await Assert.That(generated).Contains("private set");
		await Assert.That(generated).Contains("has not been initialized. Call Build first.");
		await Assert
			.That(generated)
			.Contains(
				"public override void Build([global::System.Diagnostics.CodeAnalysis.NotNull] global::Aspire.Hosting.IDistributedApplicationBuilder builder)"
			);
		await Assert.That(generated).Contains("Redis = new(redisAppResourceOptions);");
		await Assert.That(generated).Contains("Redis.IsEnabled = !hostAppOptions.IsResourceDisabled(Redis.Name);");
		await Assert.That(generated).Contains("Resources = [");
		await Assert.That(generated).Contains("base.Build(builder);");
		await Assert.That(generated).Contains("sealed partial class TestingHostAppOptions");
		await Assert.That(generated).Contains("IsResourceDisabled");
		await Assert.That(generated).Contains("static class TestingHostAppBuilderExtensions");
		await Assert.That(generated).Contains("AddTestingHostAppResourceKit");
		await Assert.That(generated).Contains("AddOptions<TestingHostAppOptions>()");
		await Assert.That(generated).Contains(".BindConfiguration(TestingHostAppOptions.SectionName)");
		await Assert.That(generated).Contains(".ValidateOnStart();");
		await Assert.That(generated).Contains("builder.Services.AddOptions<RedisAppResourceOptions>()");
		await Assert.That(generated).Contains(".BindConfiguration(RedisAppResourceOptions.SectionName)");
		await Assert
			.That(generated)
			.Contains(
				"var redisAppResourceOptions = builder.Configuration.GetSection(RedisAppResourceOptions.SectionName).Get<RedisAppResourceOptions>() ?? new RedisAppResourceOptions();"
			);
		await Assert.That(generated.Contains("public RedisAppResource()", StringComparison.Ordinal)).IsFalse();
		await Assert
			.That(generated)
			.Contains(
				"new (builder.Configuration.GetSection(TestingHostAppOptions.SectionName).Get<TestingHostAppOptions>() ?? new TestingHostAppOptions())"
			);
		await Assert
			.That(generated)
			.Contains(
				"[global::System.Diagnostics.CodeAnalysis.NotNull] this global::Aspire.Hosting.IDistributedApplicationBuilder builder"
			);
		await Assert
			.That(generated)
			.Contains(
				"global::Testing.TestingHostApp hostApp = new (builder.Configuration.GetSection(TestingHostAppOptions.SectionName).Get<TestingHostAppOptions>() ?? new TestingHostAppOptions());"
			);
		await Assert.That(generated).Contains("hostApp.Build(builder);");
		await Assert.That(generated).Contains("hostApp.Configure();");
		await Assert.That(generated).Contains("builder.Services.AddSingleton(hostApp);");
		await Assert.That(generated.Contains("Initialize(", StringComparison.Ordinal)).IsFalse();
	}

	[Test]
	public async Task Generate_GivenHostAppWithGenerateOptionsDisabled_DoesNotEmitOptionsClass(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[HostApp(GenerateOptions = false)]
	public partial class TestingHostApp { }

	[ResourceDefinition(""redis"")]
	public partial class RedisAppResource : TestingHostAppResourceBase<object> { }
}
";

		var (result, _) = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = GetGeneratedSource(result);
		await Assert
			.That(generated)
			.Contains(
				"partial class TestingHostApp : global::Purview.Aspire.ResourceKit.HostAppBase<global::Testing.TestingHostApp>"
			);
		await Assert.That(generated.Contains("TestingHostAppOptions", StringComparison.Ordinal)).IsFalse();
		await Assert.That(generated).Contains("AddOptions<RedisAppResourceOptions>()");
		await Assert.That(generated.Contains("AddOptions<TestingHostAppOptions>", StringComparison.Ordinal)).IsFalse();
	}

	[Test]
	public async Task Generate_GivenGenericResourceDefinitionWithoutExplicitBase_AutoInheritsGeneratedHostResourceBase(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[HostApp]
	public partial class TestingHostApp { }

	[ResourceDefinition<global::Aspire.Hosting.ApplicationModel.Resource>(""redis"")]
	public partial class RedisAppResource
	{
		protected override global::Aspire.Hosting.ApplicationModel.IResourceBuilder<global::Aspire.Hosting.ApplicationModel.Resource> BuildResource(global::Aspire.Hosting.IDistributedApplicationBuilder builder)
			=> throw new global::System.NotImplementedException();
	}
}
";

		// Act
		var (result, _) = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = GetGeneratedSource(result);
		await Assert
			.That(generated)
			.Contains("public partial class RedisAppResource : global::Testing.TestingHostAppResourceBase<");
		await Assert.That(generated).Contains("Aspire.Hosting.ApplicationModel.Resource>");
	}

	[Test]
	public async Task Generate_GivenHostAppWithMultipleResources_GeneratesExpectedResourcesList(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[HostApp]
	public partial class TestingHostApp { }

	[ResourceDefinition(""redis"")]
	public partial class RedisAppResource : TestingHostAppResourceBase<object> { }

	[ResourceDefinition(""sql"")]
	public partial class SqlServerAppResource : TestingHostAppResourceBase<object> { }
}
";

		var (result, _) = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = GetGeneratedSource(result);
		await Assert.That(generated).Contains("public global::Testing.RedisAppResource Redis");
		await Assert.That(generated).Contains("public global::Testing.SqlServerAppResource SqlServer");
		await Assert.That(generated).Contains("Redis = new(redisAppResourceOptions);");
		await Assert.That(generated).Contains("SqlServer = new(sqlServerAppResourceOptions);");
		await Assert.That(generated).Contains("Resources = [");
		await Assert.That(generated).Contains("Redis,");
		await Assert.That(generated).Contains("SqlServer");
	}

	[Test]
	public async Task Generate_GivenResourceWithNameOverride_UsesNameOverrideWithTypeBasedProperty(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[HostApp]
	public partial class TestingHostApp { }

	[ResourceDefinition(""my-redis"")]
	public partial class RedisAppResource : TestingHostAppResourceBase<object> { }
}
";

		var (result, _) = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = GetGeneratedSource(result);
		await Assert.That(generated).Contains("public RedisAppResourceOptions() => Name = \"my-redis\";");
	}

	[Test]
	public async Task Generate_GivenResourceWithPropertyNameOverride_GeneratesCustomProperty(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[HostApp]
	public partial class TestingHostApp { }

	[ResourceDefinition(PropertyName = ""MyRedis"")]
	public partial class RedisAppResource : TestingHostAppResourceBase<object> { }
}
";

		var (result, _) = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = GetGeneratedSource(result);
		await Assert.That(generated).Contains("public global::Testing.RedisAppResource MyRedis");
		await Assert.That(generated).Contains("public const string SectionName = \"MyRedis\";");
		await Assert.That(generated).Contains("MyRedis.IsEnabled = !hostAppOptions.IsResourceDisabled(MyRedis.Name);");
	}

	[Test]
	public async Task Generate_GivenResourceWithoutName_InitializesEmptyNameButTypeBasedProperty(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[HostApp]
	public partial class TestingHostApp { }

	[ResourceDefinition]
	public partial class RedisAppResource : TestingHostAppResourceBase<object> { }

	[ResourceDefinition]
	public partial class SqlServerResource : TestingHostAppResourceBase<object> { }
}
";

		var (result, _) = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = GetGeneratedSource(result);
		await Assert.That(generated).Contains("public RedisAppResourceOptions() => Name = \"Redis\";");
		await Assert.That(generated).Contains("public SqlServerResourceOptions() => Name = \"SqlServer\";");
	}

	[Test]
	public async Task Generate_GivenResourceNameWithSeparators_DerivesTypeBasedPropertyName(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[HostApp]
	public partial class TestingHostApp { }

	[ResourceDefinition(""azure-storage"")]
	public partial class AzureStorageAppResource : TestingHostAppResourceBase<object> { }
}
";

		var (result, _) = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = GetGeneratedSource(result);
		await Assert.That(generated).Contains("public global::Testing.AzureStorageAppResource AzureStorage");
		await Assert.That(generated).Contains("AzureStorage = new(azureStorageAppResourceOptions);");
	}

	[Test]
	public async Task Generate_GivenResourceTypeEndsWithResourceKitOrKit_AutoPropertyNameTrimsSuffix(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source =
			@"
namespace Testing
{
	[HostApp]
	public partial class TestingHostApp { }

	[ResourceDefinition]
	public partial class CacheResourceKit : TestingHostAppResourceBase<object> { }

	[ResourceDefinition]
	public partial class SecretsKit : TestingHostAppResourceBase<object> { }
}
";

		// Act
		var (result, _) = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = GetGeneratedSource(result);
		await Assert.That(generated).Contains("public global::Testing.CacheResourceKit Cache");
		await Assert.That(generated).Contains("public global::Testing.SecretsKit Secrets");
		await Assert.That(generated).Contains("public const string SectionName = \"Cache\";");
		await Assert.That(generated).Contains("public const string SectionName = \"Secrets\";");
	}

	[Test]
	public async Task Generate_GivenLowercaseResourceName_AutoPropertyNameUsesResourceTypeCasing(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing
{
	[HostApp]
	public partial class TestingHostApp { }

	[ResourceDefinition(""keyvault"")]
	public partial class KeyVaultAppResource : TestingHostAppResourceBase<object> { }
}
";

		var (result, _) = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = GetGeneratedSource(result);
		await Assert.That(generated).Contains("public global::Testing.KeyVaultAppResource KeyVault");
		await Assert.That(generated).Contains("KeyVault = new(keyVaultAppResourceOptions);");
	}

	[Test]
	public async Task Generate_GivenResourcesInDifferentNamespace_GeneratesHostNamespaceAndQualifiedResourceProperties(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
namespace Testing.Host
{
	[HostApp]
	public partial class TestingHostApp { }
}

namespace Testing.Resources
{
	[ResourceDefinition(""redis"")]
	public partial class RedisAppResource : TestingHostAppResourceBase<object> { }
}
";

		var (result, _) = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = GetGeneratedSource(result);
		await Assert.That(generated).Contains("namespace Testing.Host");
		await Assert.That(generated).Contains("public global::Testing.Resources.RedisAppResource Redis");
		await Assert.That(generated).Contains("Redis = new(redisAppResourceOptions);");
		await Assert.That(generated).Contains("abstract class TestingHostAppResourceBase<TResource>");
	}

	[Test]
	public async Task Generate_GivenHostAppWithNameOverride_UsesCustomBaseClassName(CancellationToken cancellationToken)
	{
		const string source =
			@"
namespace Testing
{
	[HostApp(Name = ""Custom"")]
	public partial class TestingHostApp;

	[ResourceDefinition(""redis"")]
	public partial class RedisAppResource : CustomResourceBase<object> { }
}
";

		var (result, _) = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = GetGeneratedSource(result);
		await Assert.That(generated).Contains("abstract class CustomResourceBase<TResource>");
		await Assert.That(generated).Contains("sealed partial class TestingHostAppOptions");
		await Assert.That(generated).Contains("ValidateOnStart();");
		await Assert.That(generated).Contains("AddTestingHostAppResourceKit");
		await Assert.That(generated).Contains("public global::Testing.RedisAppResource Redis");
	}

	[Test]
	public async Task Generate_GivenHostAppInGlobalNamespace_GeneratesWithoutNamespaceWrapper(
		CancellationToken cancellationToken
	)
	{
		const string source =
			@"
[HostApp]
public partial class GlobalHostApp { }

[ResourceDefinition(""redis"")]
public partial class RedisAppResource : GlobalHostAppResourceBase<object> { }
";

		var (result, _) = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = GetGeneratedSource(result);
		await Assert.That(generated).Contains("partial class GlobalHostApp(GlobalHostAppOptions hostAppOptions)");
		await Assert.That(generated).Contains("abstract class GlobalHostAppResourceBase<TResource>");
		await Assert.That(generated).Contains("AddGlobalHostAppResourceKit");
		// A host app declared in the global namespace must not be wrapped in a namespace block.
		await Assert.That(generated.Contains("namespace ", StringComparison.Ordinal)).IsFalse();
	}
}

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

	[AppResource(Name = ""redis"")]
	public partial class RedisAppResource : TestingHostAppResourceBase<object> { }
}
";

		var (result, _) = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();
		await Assert.That(ExcludeGenAttribs(result)).Count().IsEqualTo(1);

		var generated = GetGeneratedSource(result);
		await Assert.That(generated.Contains("partial class RedisAppResource", StringComparison.Ordinal)).IsFalse();
		await Assert.That(generated).Contains("abstract class TestingHostAppResourceBase<TResource>");
		await Assert
			.That(generated)
			.Contains("global::Purview.Aspire.ResourceKit.HostResourceBase<global::Testing.TestingHostApp, TResource>");
		await Assert
			.That(generated)
			.Contains(
				"partial class TestingHostApp(TestingHostAppOptions hostAppOptions) : global::Purview.Aspire.ResourceKit.HostAppBase<global::Testing.TestingHostApp>"
			);
		await Assert
			.That(generated)
			.Contains("public global::Testing.RedisAppResource Redis { get; } = new() { Name = \"redis\" };");
		await Assert
			.That(generated)
			.Contains(
				"public override void Build([global::System.Diagnostics.CodeAnalysis.NotNull] global::Aspire.Hosting.IDistributedApplicationBuilder builder)"
			);
		await Assert.That(generated).Contains("Redis.IsEnabled = !hostAppOptions.IsResourceDisabled(Redis.Name);");
		await Assert.That(generated).Contains("Resources = [");
		await Assert.That(generated).Contains("base.Build(builder);");
		await Assert.That(generated).Contains("sealed partial class TestingHostAppOptions");
		await Assert.That(generated).Contains("IsResourceDisabled");
		await Assert.That(generated).Contains("static class TestingHostAppBuilderExtensions");
		await Assert.That(generated).Contains("AddTestingHostAppResourceKit");
		await Assert
			.That(generated)
			.Contains("var options = builder.Configuration.GetSection(TestingHostAppOptions.SectionName)");
		await Assert.That(generated).Contains(".Get<TestingHostAppOptions>() ?? new TestingHostAppOptions();");
		await Assert
			.That(generated)
			.Contains(
				"[global::System.Diagnostics.CodeAnalysis.NotNull] this global::Aspire.Hosting.IDistributedApplicationBuilder builder"
			);
		await Assert.That(generated).Contains("global::Testing.TestingHostApp hostApp = new (options);");
		await Assert.That(generated).Contains("hostApp.Build(builder);");
		await Assert.That(generated).Contains("hostApp.Configure();");
		await Assert.That(generated).Contains("builder.Services.AddSingleton(hostApp);");
		await Assert.That(generated.Contains("Initialize(", StringComparison.Ordinal)).IsFalse();
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

	[AppResource(Name = ""redis"")]
	public partial class RedisAppResource : TestingHostAppResourceBase<object> { }

	[AppResource(Name = ""sql"")]
	public partial class SqlServerAppResource : TestingHostAppResourceBase<object> { }
}
";

		var (result, _) = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = GetGeneratedSource(result);
		await Assert
			.That(generated)
			.Contains("public global::Testing.RedisAppResource Redis { get; } = new() { Name = \"redis\" };");
		await Assert
			.That(generated)
			.Contains("public global::Testing.SqlServerAppResource SqlServer { get; } = new() { Name = \"sql\" };");
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

	[AppResource(Name = ""my-redis"")]
	public partial class RedisAppResource : TestingHostAppResourceBase<object> { }
}
";

		var (result, _) = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = GetGeneratedSource(result);
		await Assert
			.That(generated)
			.Contains("public global::Testing.RedisAppResource Redis { get; } = new() { Name = \"my-redis\" };");
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

	[AppResource(PropertyName = ""MyRedis"")]
	public partial class RedisAppResource : TestingHostAppResourceBase<object> { }
}
";

		var (result, _) = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = GetGeneratedSource(result);
		await Assert.That(generated).Contains("public global::Testing.RedisAppResource MyRedis { get; }");
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

	[AppResource]
	public partial class RedisAppResource : TestingHostAppResourceBase<object> { }

	[AppResource]
	public partial class SqlServerResource : TestingHostAppResourceBase<object> { }
}
";

		var (result, _) = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = GetGeneratedSource(result);
		await Assert
			.That(generated)
			.Contains("public global::Testing.RedisAppResource Redis { get; } = new() { Name = \"\" };");
		await Assert
			.That(generated)
			.Contains("public global::Testing.SqlServerResource SqlServer { get; } = new() { Name = \"\" };");
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

	[AppResource(Name = ""azure-storage"")]
	public partial class AzureStorageAppResource : TestingHostAppResourceBase<object> { }
}
";

		var (result, _) = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = GetGeneratedSource(result);
		await Assert
			.That(generated)
			.Contains(
				"public global::Testing.AzureStorageAppResource AzureStorage { get; } = new() { Name = \"azure-storage\" };"
			);
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

	[AppResource(Name = ""keyvault"")]
	public partial class KeyVaultAppResource : TestingHostAppResourceBase<object> { }
}
";

		var (result, _) = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = GetGeneratedSource(result);
		await Assert
			.That(generated)
			.Contains("public global::Testing.KeyVaultAppResource KeyVault { get; } = new() { Name = \"keyvault\" };");
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
	[AppResource(Name = ""redis"")]
	public partial class RedisAppResource : TestingHostAppResourceBase<object> { }
}
";

		var (result, _) = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = GetGeneratedSource(result);
		await Assert.That(generated).Contains("namespace Testing.Host");
		await Assert
			.That(generated)
			.Contains("public global::Testing.Resources.RedisAppResource Redis { get; } = new() { Name = \"redis\" };");
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

	[AppResource(Name = ""redis"")]
	public partial class RedisAppResource : CustomResourceBase<object> { }
}
";

		var (result, _) = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = GetGeneratedSource(result);
		await Assert.That(generated).Contains("abstract class CustomResourceBase<TResource>");
		await Assert.That(generated).Contains("sealed partial class TestingHostAppOptions");
		await Assert.That(generated).Contains("AddTestingHostAppResourceKit");
		await Assert
			.That(generated)
			.Contains("public global::Testing.RedisAppResource Redis { get; } = new() { Name = \"redis\" };");
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

[AppResource(Name = ""redis"")]
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

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Asserts the structure and content of the host app source emitted by
/// <see cref="HostAppGenerator"/> for a range of host app / app resource configurations.
/// </summary>
public class GeneratedSourceContentTests : IncrementalSourceGeneratorTestBase<HostAppGenerator>
{
	[Test]
	public async Task Generate_GivenHostAppWithSingleResource_GeneratesBaseClassPartialOptionsAndExtensions(
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
		await Assert.That(generated).Contains("partial class RedisAppResource");
		await Assert.That(generated).Contains(@"public override string Name => ""redis"";");
		await Assert.That(generated).Contains("abstract class TestingHostAppResourceBase<TResource>");
		await Assert.That(generated).Contains("partial class TestingHostApp");
		await Assert
			.That(generated)
			.Contains("public global::Testing.RedisAppResource Redis { get; private set; } = default!;");
		await Assert
			.That(generated)
			.Contains("public void Initialize(global::System.IServiceProvider serviceProvider)");
		await Assert
			.That(generated)
			.Contains("public void Build(global::Aspire.Hosting.IDistributedApplicationBuilder builder)");
		await Assert.That(generated).Contains("public void Configure()");
		await Assert.That(generated).Contains("sealed partial class TestingHostAppOptions");
		await Assert.That(generated).Contains("IsResourceDisabled");
		await Assert.That(generated).Contains("static class TestingHostAppBuilderExtensions");
		await Assert.That(generated).Contains("AddTestingHostApp");
		await Assert.That(generated).Contains("ServiceLifetime.Singleton");
	}

	[Test]
	public async Task Generate_GivenHostAppWithMultipleResources_GeneratesAllResourceProperties(
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
		await Assert.That(generated).Contains("public global::Testing.RedisAppResource Redis { get; private set; }");
		await Assert.That(generated).Contains("public global::Testing.SqlServerAppResource Sql { get; private set; }");
		await Assert.That(generated).Contains("Redis.Build(builder);");
		await Assert.That(generated).Contains("Sql.Build(builder);");
		await Assert.That(generated).Contains("Redis.Configure(this);");
		await Assert.That(generated).Contains("Sql.Configure(this);");
	}

	[Test]
	public async Task Generate_GivenResourceWithNameOverride_GeneratesNameOverrideInPartial(
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
		await Assert.That(generated).Contains(@"public override string Name => ""my-redis"";");
		await Assert.That(generated).Contains("public global::Testing.RedisAppResource MyRedis { get; private set; }");
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
		await Assert.That(generated).Contains(@"public override string Name => ""Redis"";");
		await Assert.That(generated).Contains("public global::Testing.RedisAppResource MyRedis { get; private set; }");
		await Assert
			.That(generated)
			.Contains(
				"MyRedis = ActivatorUtilities.GetServiceOrCreateInstance<global::Testing.RedisAppResource>(serviceProvider);"
			);
	}

	[Test]
	public async Task Generate_GivenResourceWithoutName_DerivesNameFromTypeName(CancellationToken cancellationToken)
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
		await Assert.That(generated).Contains(@"public override string Name => ""Redis"";");
		await Assert.That(generated).Contains(@"public override string Name => ""SqlServer"";");
		await Assert.That(generated).Contains("public global::Testing.RedisAppResource Redis { get; private set; }");
		await Assert
			.That(generated)
			.Contains("public global::Testing.SqlServerResource SqlServer { get; private set; }");
	}

	[Test]
	public async Task Generate_GivenResourceNameWithSeparators_DerivesPascalCasePropertyName(
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
		await Assert.That(generated).Contains(@"public override string Name => ""azure-storage"";");
		await Assert
			.That(generated)
			.Contains("public global::Testing.AzureStorageAppResource AzureStorage { get; private set; }");
	}

	[Test]
	public async Task Generate_GivenResourcesInDifferentNamespace_GeneratesNamespacedPartials(
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
		await Assert.That(generated).Contains("namespace Testing.Resources");
		await Assert.That(generated).Contains("namespace Testing.Host");
		await Assert
			.That(generated)
			.Contains("public global::Testing.Resources.RedisAppResource Redis { get; private set; } = default!;");
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
	public partial class TestingHostApp { }

	[AppResource(Name = ""redis"")]
	public partial class RedisAppResource : CustomResourceBase<object> { }
}
";

		var (result, _) = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = GetGeneratedSource(result);
		await Assert.That(generated).Contains("abstract class CustomResourceBase<TResource>");
		await Assert.That(generated).Contains("sealed partial class TestingHostAppOptions");
		await Assert.That(generated).Contains("AddTestingHostApp");
		await Assert.That(generated).Contains(@"public override string Name => ""redis"";");
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
		await Assert.That(generated).Contains("partial class GlobalHostApp");
		await Assert.That(generated).Contains("abstract class GlobalHostAppResourceBase<TResource>");
		await Assert.That(generated).Contains("AddGlobalHostApp");
		// A host app declared in the global namespace must not be wrapped in a namespace block.
		await Assert.That(generated.Contains("namespace ", StringComparison.Ordinal)).IsFalse();
	}
}

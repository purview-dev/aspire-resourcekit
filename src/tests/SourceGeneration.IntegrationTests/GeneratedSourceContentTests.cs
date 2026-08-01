using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Asserts the structure and content of the host app source emitted by
/// <see cref="HostKitGenerator"/> for a range of host app / app resource configurations.
/// </summary>
public class GeneratedSourceContentTests : IncrementalSourceGeneratorTestBase<HostKitGenerator>
{
	[Test]
	public async Task Generate_GivenHostKitWithSingleResource_GeneratesExpectedHostKitSource(
		CancellationToken cancellationToken
	)
	{
		var source =
			@$"
namespace Testing
{{
	[HostKit]
	public partial class TestingHostKit;

	[ResourceDefinition(""redis"")]
	public partial class RedisResourceKit : {TypeHelpers.ResourceKitBase.TypeName}<{TestHelper.DefaultAspireResource}>
	{{
		{TestHelper.GenerateBuildResource()}
	}}
}}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();
		await Assert.That(result.NonAttributeSyntaxTrees.Count()).IsEqualTo(1);

		var generated = await result.GetSourceAsync(cancellationToken);
		await Assert.That(generated).Contains("partial class RedisResourceKit");
		await Assert
			.That(generated)
			.Contains($"abstract partial class {TypeHelpers.ResourceKitBase.TypeName}<TResource>");
		await Assert
			.That(generated)
			.Contains("global::Purview.Aspire.ResourceKit.ResourceKitBase<global::Testing.TestingHostKit, TResource>");
		await Assert.That(generated).Contains("public sealed partial class RedisResourceKitOptions");
		await Assert
			.That(generated)
			.Contains("public global::Testing.RedisResourceKit.RedisResourceKitOptions Options { get; }");
		await Assert
			.That(generated)
			.Contains("public global::Testing.RedisResourceKit.RedisResourceKitOptions Redis { get; set; } = new();");
		await Assert
			.That(generated.Contains("public const string SectionName = \"Redis\";", StringComparison.Ordinal))
			.IsFalse();
		await Assert
			.That(generated)
			.Contains("protected ResourceKitBase(global::Testing.TestingHostKit hostKit, string? name)");
		await Assert
			.That(generated)
			.Contains(
				": base(hostKit, (options ?? throw new global::System.ArgumentNullException(nameof(options))).Name)"
			);
		await Assert.That(generated).Contains("IsEnabled = options.IsEnabled;");
		await Assert
			.That(generated)
			.Contains("[global::System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = false)]");
		await Assert.That(generated).Contains("public string Name { get; set; } = \"redis\";");
		await Assert.That(generated).Contains("public bool IsEnabled { get; set; } = true;");
		await Assert
			.That(generated)
			.Contains(
				"partial class TestingHostKit(global::Testing.TestingHostKit.TestingHostKitOptions options) : global::Purview.Aspire.ResourceKit.HostKitBase<global::Testing.TestingHostKit>"
			);
		await Assert.That(generated).Contains("public global::Testing.RedisResourceKit Redis");
		await Assert.That(generated).Contains("private set");
		await Assert.That(generated).Contains("has not been initialized. Call Build first.");
		await Assert
			.That(generated)
			.Contains("public override void Build(global::Aspire.Hosting.IDistributedApplicationBuilder builder)");
		await Assert.That(generated).Contains("Redis = new(this, Options.Redis);");
		await Assert.That(generated).Contains("Resources = [");
		await Assert.That(generated).Contains("base.Build(builder);");
		await Assert.That(generated).Contains("sealed partial class TestingHostKitOptions");
		await Assert.That(generated).Contains("static class TestingHostKitBuilderExtensions");
		await Assert.That(generated).Contains("AddAspireResourceKit");
		await Assert.That(generated).Contains("AddOptions<global::Testing.TestingHostKit.TestingHostKitOptions>()");
		await Assert
			.That(generated)
			.Contains(".BindConfiguration(global::Testing.TestingHostKit.TestingHostKitOptions.SectionName)");
		await Assert.That(generated).Contains(".ValidateOnStart();");
		await Assert
			.That(
				generated.Contains(
					"builder.Services.AddOptions<global::Testing.RedisResourceKit.RedisResourceKitOptions>()",
					StringComparison.Ordinal
				)
			)
			.IsFalse();
		await Assert
			.That(
				generated.Contains(
					"var redisResourceKitOptions = builder.Configuration.GetSection",
					StringComparison.Ordinal
				)
			)
			.IsFalse();
		await Assert.That(generated.Contains("public RedisResourceKit()", StringComparison.Ordinal)).IsFalse();
		await Assert
			.That(generated)
			.Contains(
				"var hostKitOptions = builder.Configuration.GetSection(global::Testing.TestingHostKit.TestingHostKitOptions.SectionName).Get<global::Testing.TestingHostKit.TestingHostKitOptions>() ?? new();"
			);
		await Assert.That(generated).Contains("this global::Aspire.Hosting.IDistributedApplicationBuilder builder");
		await Assert.That(generated).Contains("global::Testing.TestingHostKit hostKit = new (hostKitOptions);");
		await Assert.That(generated).Contains("hostKit.Build(builder);");
		await Assert.That(generated).Contains("hostKit.Configure();");
		await Assert.That(generated).Contains("builder.Services.AddSingleton(hostKit);");
		await Assert.That(generated.Contains("Initialize(", StringComparison.Ordinal)).IsFalse();
	}

	[Test]
	public async Task Generate_GivenHostKitWithGenerateOptionsDisabled_DoesNotEmitOptionsClass(
		CancellationToken cancellationToken
	)
	{
		var source =
			@$"
namespace Testing
{{
	[HostKit(GenerateOptions = false)]
	public partial class TestingHostKit;

	[ResourceDefinition(""redis"")]
	public partial class RedisResourceKit : {TypeHelpers.ResourceKitBase.TypeName}<{TestHelper.DefaultAspireResource}>
	{{
		{TestHelper.GenerateBuildResource()}
	}}
}}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = await result.GetSourceAsync(cancellationToken);
		await Assert
			.That(generated)
			.Contains($"partial class TestingHostKit : {TypeHelpers.HostKitBase}<global::Testing.TestingHostKit>");
		await Assert.That(generated).DoesNotContain("TestingHostKitOptions()");
		await Assert.That(generated).DoesNotContain("RedisResourceKitOptions()");
	}

	[Test]
	public async Task Generate_GivenGenericResourceDefinitionWithoutExplicitBase_AutoInheritsGeneratedHostResourceBase(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var source =
			@$"
namespace Testing
{{
	[HostKit]
	public partial class TestingHostKit;

	[ResourceDefinition<{TestHelper.DefaultAspireResource}>(""redis"")]
	public partial class RedisResourceKit
	{{
		{TestHelper.GenerateBuildResource()}
	}}
}}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = await result.GetSourceAsync(cancellationToken);
		await Assert
			.That(generated)
			.Contains(
				$"partial class RedisResourceKit : {TypeHelpers.ResourceKitBase}<{TestHelper.DefaultAspireResource}>"
			);
	}

	[Test]
	public async Task Generate_GivenExplicitBaseClass_CorrectlyGenerates(CancellationToken cancellationToken)
	{
		// Arrange
		var source =
			@$"
namespace Testing
{{
	[HostKit]
	public partial class TestingHostKit;

	[ResourceDefinition]
	public partial class RedisResourceKit : {TypeHelpers.ResourceKitBase}<{TestHelper.DefaultAspireResource}>
	{{
		{TestHelper.GenerateBuildResource()}
	}}
}}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = await result.GetSourceAsync(cancellationToken);
		await Assert.That(generated).Contains($"public RedisResourceKit(");
	}

	[Test]
	public async Task Generate_GivenNoExplicitBaseClass_CorrectlyGenerates(CancellationToken cancellationToken)
	{
		// Arrange
		var source =
			@$"
namespace Testing
{{
	[HostKit]
	public partial class TestingHostKit;

	[ResourceDefinition<{TestHelper.DefaultAspireResource}>]
	public partial class RedisResourceKit
	{{
		{TestHelper.GenerateBuildResource()}
	}}
}}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = await result.GetSourceAsync(cancellationToken);
		await Assert
			.That(generated)
			.Contains(
				$"partial class RedisResourceKit : {TypeHelpers.ResourceKitBase}<{TestHelper.DefaultAspireResource}>"
			);
	}

	[Test]
	public async Task Generate_GivenHostKitWithMultipleResources_GeneratesExpectedResourcesList(
		CancellationToken cancellationToken
	)
	{
		var buildResourceMethod = TestHelper.GenerateBuildResource();
		var source =
			@$"
namespace Testing
{{
	[HostKit]
	public partial class TestingHostKit;

	[ResourceDefinition(""redis"")]
	public partial class RedisResourceKit : {TypeHelpers.ResourceKitBase.TypeName}<{TestHelper.DefaultAspireResource}>
	{{
		{buildResourceMethod}
	}}

	[ResourceDefinition(""sql"")]
	public partial class SqlServerResourceKit : {TypeHelpers.ResourceKitBase.TypeName}<{TestHelper.DefaultAspireResource}>
	{{
		{buildResourceMethod}
	}}
}}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = await result.GetSourceAsync(cancellationToken);
		await Assert.That(generated).Contains("public global::Testing.RedisResourceKit Redis");
		await Assert.That(generated).Contains("public global::Testing.SqlServerResourceKit SqlServer");
		await Assert.That(generated).Contains("Redis = new(this, Options.Redis);");
		await Assert.That(generated).Contains("SqlServer = new(this, Options.SqlServer);");
		await Assert.That(generated).Contains("Resources = [");
		await Assert.That(generated).Contains("Redis,");
		await Assert.That(generated).Contains("SqlServer");
	}

	[Test]
	public async Task Generate_GivenResourceWithNameOverride_UsesNameOverrideWithTypeBasedProperty(
		CancellationToken cancellationToken
	)
	{
		var source =
			@$"
namespace Testing
{{
	[HostKit]
	public partial class TestingHostKit;

	[ResourceDefinition(""my-redis"")]
	public partial class RedisResourceKit : {TypeHelpers.ResourceKitBase}<{TestHelper.DefaultAspireResource}>
	{{
		{TestHelper.GenerateBuildResource()}
	}}
}}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = await result.GetSourceAsync(cancellationToken);
		await Assert
			.That(generated.Contains("public const string SectionName = \"Redis\";", StringComparison.Ordinal))
			.IsFalse();
	}

	[Test]
	public async Task Generate_GivenResourceWithPropertyNameOverride_GeneratesCustomProperty(
		CancellationToken cancellationToken
	)
	{
		var source =
			@$"
namespace Testing
{{
	[HostKit]
	public partial class TestingHostKit;

	[ResourceDefinition(PropertyName = ""MyRedis"")]
	public partial class RedisResourceKit : {TypeHelpers.ResourceKitBase}<{TestHelper.DefaultAspireResource}>
	{{
		{TestHelper.GenerateBuildResource()}
	}}
}}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = await result.GetSourceAsync(cancellationToken);
		await Assert.That(generated).Contains("public global::Testing.RedisResourceKit MyRedis");
		await Assert
			.That(generated.Contains("public const string SectionName = \"MyRedis\";", StringComparison.Ordinal))
			.IsFalse();
		await Assert.That(generated).Contains("MyRedis = new(this, Options.MyRedis);");
	}

	[Test]
	public async Task Generate_GivenResourceWithoutName_InitializesEmptyNameButTypeBasedProperty(
		CancellationToken cancellationToken
	)
	{
		var source =
			@$"
namespace Testing
{{
	[HostKit]
	public partial class TestingHostKit;

	[ResourceDefinition]
	public partial class RedisResourceKit : {TypeHelpers.ResourceKitBase}<{TestHelper.DefaultAspireResource}>
	{{
		{TestHelper.GenerateBuildResource()}
	}}

	[ResourceDefinition]
	public partial class SqlServerResource : {TypeHelpers.ResourceKitBase}<{TestHelper.DefaultAspireResource}>
	{{
		{TestHelper.GenerateBuildResource()}
	}}
}}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = await result.GetSourceAsync(cancellationToken);
		await Assert
			.That(generated.Contains("public const string SectionName = \"Redis\";", StringComparison.Ordinal))
			.IsFalse();
		await Assert
			.That(generated.Contains("public const string SectionName = \"SqlServer\";", StringComparison.Ordinal))
			.IsFalse();
	}

	[Test]
	public async Task Generate_GivenResourceNameWithSeparators_DerivesTypeBasedPropertyName(
		CancellationToken cancellationToken
	)
	{
		var source =
			@$"
namespace Testing
{{
	[HostKit]
	public partial class TestingHostKit;

	[ResourceDefinition(""azure-storage"")]
	public partial class AzureStorageResourceKit : {TypeHelpers.ResourceKitBase}<{TestHelper.DefaultAspireResource}>
	{{
		{TestHelper.GenerateBuildResource()}
	}}
}}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = await result.GetSourceAsync(cancellationToken);
		await Assert.That(generated).Contains("public global::Testing.AzureStorageResourceKit AzureStorage");
		await Assert.That(generated).Contains("AzureStorage = new(this, Options.AzureStorage);");
	}

	[Test]
	public async Task Generate_GivenResourceTypeEndsWithResourceKitOrKit_AutoPropertyNameTrimsSuffix(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var source =
			@$"
namespace Testing
{{
	[HostKit]
	public partial class TestingHostKit;

	[ResourceDefinition]
	public partial class CacheResourceKit : {TypeHelpers.ResourceKitBase}<{TestHelper.DefaultAspireResource}>
	{{
		{TestHelper.GenerateBuildResource()}
	}}

	[ResourceDefinition]
	public partial class SecretsKit : {TypeHelpers.ResourceKitBase}<{TestHelper.DefaultAspireResource}>
	{{
		{TestHelper.GenerateBuildResource()}
	}}
}}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = await result.GetSourceAsync(cancellationToken);
		await Assert.That(generated).Contains("public global::Testing.CacheResourceKit Cache");
		await Assert.That(generated).Contains("public global::Testing.SecretsKit Secrets");
		await Assert
			.That(generated.Contains("public const string SectionName = \"Cache\";", StringComparison.Ordinal))
			.IsFalse();
		await Assert
			.That(generated.Contains("public const string SectionName = \"Secrets\";", StringComparison.Ordinal))
			.IsFalse();
	}

	[Test]
	public async Task Generate_GivenLowercaseResourceName_AutoPropertyNameUsesResourceTypeCasing(
		CancellationToken cancellationToken
	)
	{
		var source =
			@$"
namespace Testing
{{
	[HostKit]
	public partial class TestingHostKit;

	[ResourceDefinition(""keyvault"")]
	public partial class KeyVaultResourceKit : {TypeHelpers.ResourceKitBase.TypeName}<{TestHelper.DefaultAspireResource}>
	{{
		 {TestHelper.GenerateBuildResource()}
	}}
}}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = await result.GetSourceAsync(cancellationToken);
		await Assert.That(generated).Contains("public global::Testing.KeyVaultResourceKit KeyVault");
		await Assert.That(generated).Contains("KeyVault = new(this, Options.KeyVault);");
	}

	[Test]
	public async Task Generate_GivenResourcesInDifferentNamespace_GeneratesHostNamespaceAndQualifiedResourceProperties(
		CancellationToken cancellationToken
	)
	{
		var source =
			@$"
namespace Testing.Host
{{
	[HostKit]
	public partial class TestingHostKit;
}}

namespace Testing.Resources
{{
	[ResourceDefinition(""redis"")]
	public partial class RedisResourceKit : {TypeHelpers.ResourceKitBase}<{TestHelper.DefaultAspireResource}>
	{{
		 {TestHelper.GenerateBuildResource()}
	}}
}}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var assembly = await Assert.That(result.Assembly).IsNotNull();
		var types = assembly.GetExportedTypes();

		var hostKitType = await Assert.That(types).HasSingleItem(m => m.Name == "TestingHostKit");
		var redisResourceKitType = await Assert.That(types).HasSingleItem(m => m.Name == "RedisResourceKit");

		await Assert.That(hostKitType.Namespace).IsEqualTo("Testing.Host");
		await Assert.That(redisResourceKitType.Namespace).IsEqualTo("Testing.Resources");
	}

	[Test]
	[Skip(
		"This test is skipped because the current implementation does not support custom base class names for HostKit."
	)]
	public async Task Generate_GivenHostKitWithNameOverride_UsesCustomBaseClassName(CancellationToken cancellationToken)
	{
		var source =
			@$"
namespace Testing
{{
	[HostKit(Name = ""Custom"")]
	public partial class TestingHostKit;

	[ResourceDefinition(""redis"")]
	public partial class RedisResourceKit : CustomResourceBase<{TestHelper.DefaultAspireResource}>
	{{
		{TestHelper.GenerateBuildResource()}
	}}

	public abstract class CustomResourceBase<TResource> : {TypeHelpers.ResourceKitBase}<TResource>
		where TResource : class, IResource
	{{
		protected CustomResourceBase(TestingHostKit hostKit, string? name) : base(hostKit, name)
		{{
		}}
	}}
}}
";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = await result.GetSourceAsync(cancellationToken);
		await Assert.That(generated).Contains("abstract class CustomResourceBase<TResource>");
		await Assert.That(generated).Contains("sealed partial class TestingHostKitOptions");
		await Assert.That(generated).Contains("ValidateOnStart();");
		await Assert.That(generated).Contains("AddAspireResourceKit");
		await Assert.That(generated).Contains("public global::Testing.RedisResourceKit Redis");
	}

	[Test]
	public async Task Generate_GivenHostKitInGlobalNamespace_GeneratesWithoutNamespaceWrapper(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string globalHostKitTypeName = "GlobalHostKit";
		const string redisResourceKitTypeName = "RedisResourceKit";

		var source =
			@$"
[HostKit]
public partial class {globalHostKitTypeName};

[ResourceDefinition(""redis"")]
public partial class {redisResourceKitTypeName} : {TypeHelpers.ResourceKitBase.TypeName}<{TestHelper.DefaultAspireResource}>
{{
	{TestHelper.GenerateBuildResource()}
}}
";

		// Act
		var result = await GenerateAsync(source, cancellationToken);
		var assembly = await Assert.That(result.Assembly).IsNotNull();
		var types = assembly.GetExportedTypes();

		var globalHostKitType = types.SingleOrDefault(m => m.Name == globalHostKitTypeName);
		var redisResourceKitType = types.SingleOrDefault(m => m.Name == redisResourceKitTypeName);

		// Assert
		await Assert.That(globalHostKitType).IsNotNull();
		await Assert.That(redisResourceKitType).IsNotNull();

		await Assert.That(globalHostKitType.Namespace).IsNull();
		await Assert.That(redisResourceKitType.Namespace).IsNull();
	}
}

using Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

namespace Purview.Aspire.ResourceKit.SourceGeneration;

/// <summary>
/// Asserts the structure and content of the host app source emitted by
/// <see cref="HostKitGenerator"/> for a range of host app / app resource configurations.
/// </summary>
public partial class GeneratedSourceContentTests : SourceGeneratorTestBase<HostKitGenerator>
{
	[Test]
	public async Task Generate_GivenHostKitWithGenerateOptionsDisabled_DoesNotEmitOptionsClass(
		CancellationToken cancellationToken
	)
	{
		var source = TestHelper.GenerateSources(generateOptions: false);

		var result = await ResourceKitGenerateAsync(source, cancellationToken);

		var generated = result.GetSource();
		await Assert.That(generated).DoesNotContain(TestHelper.DefaultHostKitType + "Options()");
		await Assert.That(generated).DoesNotContain(TestHelper.DefaultResourceKitType + "Options()");
	}

	[Test]
	public async Task Generate_GivenGenericResourceDefinitionWithoutExplicitBase_AutoInheritsGeneratedHostResourceBase(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var source = TestHelper.GenerateSources(resourceKitBaseClass: null);

		// Act
		var result = await ResourceKitGenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
		await Assert.That(generated).Contains($"{TypeLibrary.ResourceKitBase}<{TestHelper.DefaultAspireResource}>");
	}

	[Test]
	public async Task Generate_GivenExplicitBaseClass_CorrectlyGenerates(CancellationToken cancellationToken)
	{
		// Arrange
		var sources = TestHelper.GenerateSources(
			resourceKitName: "RedisResourceKit",
			resourceKitBaseClass: TypeLibrary.ResourceKitBase
		);

		// Act
		var result = await ResourceKitGenerateAsync(sources, cancellationToken);

		// Assert
		var generated = result.GetSource();
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
		var result = await ResourceKitGenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
		await Assert
			.That(generated)
			.Contains(
				$"partial class RedisResourceKit : {TypeLibrary.ResourceKitBase}<{TestHelper.DefaultAspireResource}>"
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
using {TypeLibrary.HostKitAttribute.Namespace};
using {TypeLibrary.ResourceKitBase.Namespace};

namespace Testing
{{
	[HostKit]
	public partial class TestingHostKit;

	[ResourceDefinition(""redis"")]
	public partial class RedisResourceKit : {TypeLibrary.ResourceKitBase.TypeName}<{TestHelper.DefaultAspireResource}>
	{{
		{buildResourceMethod}
	}}

	[ResourceDefinition(""sql"")]
	public partial class SqlServerResourceKit : {TypeLibrary.ResourceKitBase.TypeName}<{TestHelper.DefaultAspireResource}>
	{{
		{buildResourceMethod}
	}}
}}
";

		var result = await ResourceKitGenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
		await Assert.That(generated).Contains("public global::Testing.RedisResourceKit Redis");
		await Assert.That(generated).Contains("public global::Testing.SqlServerResourceKit SqlServer");
		await Assert.That(generated).Contains("Redis = new(this, Options.Redis);");
		await Assert.That(generated).Contains("SqlServer = new(this, Options.SqlServer);");
		await Assert.That(generated).Contains("AddResource(Redis);");
		await Assert.That(generated).Contains("AddResource(SqlServer);");
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
	public partial class RedisResourceKit : {TypeLibrary.ResourceKitBase}<{TestHelper.DefaultAspireResource}>
	{{
		{TestHelper.GenerateBuildResource()}
	}}
}}
";

		var result = await ResourceKitGenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
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
	public partial class RedisResourceKit : {TypeLibrary.ResourceKitBase}<{TestHelper.DefaultAspireResource}>
	{{
		{TestHelper.GenerateBuildResource()}
	}}
}}
";

		var result = await ResourceKitGenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
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
	public partial class RedisResourceKit : {TypeLibrary.ResourceKitBase}<{TestHelper.DefaultAspireResource}>
	{{
		{TestHelper.GenerateBuildResource()}
	}}

	[ResourceDefinition]
	public partial class SqlServerResource : {TypeLibrary.ResourceKitBase}<{TestHelper.DefaultAspireResource}>
	{{
		{TestHelper.GenerateBuildResource()}
	}}
}}
";

		var result = await ResourceKitGenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
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
	public partial class AzureStorageResourceKit : {TypeLibrary.ResourceKitBase}<{TestHelper.DefaultAspireResource}>
	{{
		{TestHelper.GenerateBuildResource()}
	}}
}}
";

		var result = await ResourceKitGenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
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
	public partial class CacheResourceKit : {TypeLibrary.ResourceKitBase}<{TestHelper.DefaultAspireResource}>
	{{
		{TestHelper.GenerateBuildResource()}
	}}

	[ResourceDefinition]
	public partial class SecretsKit : {TypeLibrary.ResourceKitBase}<{TestHelper.DefaultAspireResource}>
	{{
		{TestHelper.GenerateBuildResource()}
	}}
}}
";

		// Act
		var result = await ResourceKitGenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
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
	public partial class KeyVaultResourceKit : {TypeLibrary.ResourceKitBase.TypeName}<{TestHelper.DefaultAspireResource}>
	{{
		 {TestHelper.GenerateBuildResource()}
	}}
}}
";

		var result = await ResourceKitGenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
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
	public partial class RedisResourceKit : {TypeLibrary.ResourceKitBase}<{TestHelper.DefaultAspireResource}>
	{{
		 {TestHelper.GenerateBuildResource()}
	}}
}}
";

		var result = await ResourceKitGenerateAsync(source, cancellationToken);

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

	public abstract class CustomResourceBase<TResource> : {TypeLibrary.ResourceKitBase}<TResource>
		where TResource : class, IResource
	{{
		protected CustomResourceBase(TestingHostKit hostKit, string? name) : base(hostKit, name)
		{{
		}}
	}}
}}
";

		var result = await ResourceKitGenerateAsync(source, cancellationToken);

		await Assert.That(result).HasNoErrorDiagnostics();

		var generated = result.GetSource();
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

		var sources = TestHelper.GenerateSources(
			hostKitName: globalHostKitTypeName,
			hostKitNamespace: null,
			resourceKitName: redisResourceKitTypeName,
			resourceKitBaseClass: TypeLibrary.ResourceKitBase,
			resourceKitNamespace: null
		);

		// Act
		var result = await ResourceKitGenerateAsync(sources, cancellationToken);
		var types = result.Assembly!.GetExportedTypes();

		var globalHostKitType = types.SingleOrDefault(m => m.Name == globalHostKitTypeName);
		var redisResourceKitType = types.SingleOrDefault(m => m.Name == redisResourceKitTypeName);

		// Assert
		await Assert.That(globalHostKitType).IsNotNull();
		await Assert.That(redisResourceKitType).IsNotNull();

		await Assert.That(globalHostKitType.Namespace).IsNull();
		await Assert.That(redisResourceKitType.Namespace).IsNull();
	}
}

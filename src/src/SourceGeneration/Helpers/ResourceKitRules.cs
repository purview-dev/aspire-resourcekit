using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

/// <summary>
/// Shared rule evaluation for resource kits. Both the <see cref="ResourceKitDiagnosticAnalyzer"/>
/// (which reports the rules) and the source generator (which uses the same rules to decide whether a
/// resource kit should be generated) evaluate rules through this single set of helpers so the two never
/// drift apart.
/// </summary>
/// <remarks>
/// Rules split into two groups: generation-blocking rules (raised as errors, which stop generation via
/// <see cref="GeneratorResult{T}.ShouldProcess"/>) and execution-only rules (see
/// <see cref="ExecutionOnlyRuleIds"/>, raised as warnings so generation always proceeds).
/// </remarks>
static class ResourceKitRules
{
	/// <summary>
	/// The diagnostic IDs owned by <see cref="ResourceKitDiagnosticAnalyzer"/>. The source generator still
	/// computes these internally so its <c>ShouldProcess</c> gating stays correct, but must not re-report
	/// them or consumers would see duplicates.
	/// </summary>
	public static readonly ImmutableHashSet<string> AnalyzerOwnedRuleIds = ImmutableHashSet.Create(
		StringComparer.Ordinal,
		DiagnosticLibrary.ClassMustBePartial.Id,
		DiagnosticLibrary.NoResourceKitsDefined.Id,
		DiagnosticLibrary.MultipleHostKitsFoundInfo.Id,
		DiagnosticLibrary.DuplicateResourcePropertyName.Id,
		DiagnosticLibrary.ResourceMustDeriveFromResourceKitBase.Id,
		DiagnosticLibrary.ResourceNameNotDerivable.Id,
		DiagnosticLibrary.InvalidPropertyName.Id,
		DiagnosticLibrary.NonEmptyConstructorsNotSupported.Id,
		DiagnosticLibrary.MixedResourceDefinitionAttributesNotSupported.Id,
		DiagnosticLibrary.NonGenericResourceDefinitionRequiresExplicitBase.Id,
		DiagnosticLibrary.GenericResourceDefinitionCannotHaveExplicitBase.Id,
		DiagnosticLibrary.NoAspireResourceFound.Id,
		DiagnosticLibrary.ResourcePropertyNeverSet.Id,
		DiagnosticLibrary.ProjectDefinitionMismatch.Id,
		DiagnosticLibrary.ProjectResourceKitBaseMismatch.Id,
		DiagnosticLibrary.GroupTypeNotResourceGroup.Id,
		DiagnosticLibrary.ResourceGroupCycle.Id,
		DiagnosticLibrary.DuplicateResourceGroupName.Id,
		DiagnosticLibrary.MultiplePrimaryGroups.Id,
		DiagnosticLibrary.ResourceGroupCannotHaveExplicitBase.Id,
		DiagnosticLibrary.DuplicateUrlName.Id,
		DiagnosticLibrary.EndpointUrlRequiresEndpoints.Id,
		DiagnosticLibrary.RelativeUrlWithoutEndpoints.Id,
		DiagnosticLibrary.MultipleResourceGroupParents.Id,
		DiagnosticLibrary.ResourceGroupCannotBeGeneric.Id,
		DiagnosticLibrary.ResourceGroupCannotBeResourceKit.Id
	);

	/// <summary>
	/// A neutral rule evaluation that can be converted into either a <c>ReportableDiagnostic</c> (for the generator's
	/// incremental model) or a Roslyn <see cref="Diagnostic"/> (for the analyzer).
	/// </summary>
	internal readonly record struct RuleEvaluation(
		DiagnosticDescriptor Descriptor,
		bool IsBlocking,
		Location? Location,
		ImmutableArray<object> MessageArgs
	)
	{
		public ReportableDiagnostic ToDiagnosticInfo() =>
			ReportableDiagnostic.Create(Descriptor, IsBlocking, Location, [.. MessageArgs]);

		public Diagnostic ToDiagnostic() => Diagnostic.Create(Descriptor, Location ?? Location.None, [.. MessageArgs]);
	}

	/// <summary>
	/// The diagnostic IDs that report problems which prevent correct runtime execution but do not
	/// prevent generation. These are raised as warnings so they never flip a <c>GeneratorResult</c>'s
	/// <c>ShouldProcess</c> (and therefore never halt generation via <c>IsFatal</c>). A resource kit with
	/// an incomplete or inconsistent <c>BuildResource</c>/<c>ConfigureResource</c> wiring (a project not
	/// added via <c>AddProject&lt;T&gt;()</c>, or an <c>IResourceBuilder&lt;T&gt;</c> property never
	/// assigned) must still be generated so the user can complete the override rather than lose the
	/// whole host kit output.
	/// </summary>
	public static readonly ImmutableHashSet<string> ExecutionOnlyRuleIds = ImmutableHashSet.Create(
		StringComparer.Ordinal,
		DiagnosticLibrary.ResourcePropertyNeverSet.Id,
		DiagnosticLibrary.ProjectDefinitionMismatch.Id,
		DiagnosticLibrary.ProjectResourceKitBaseMismatch.Id,
		DiagnosticLibrary.MultiplePrimaryGroups.Id,
		DiagnosticLibrary.RelativeUrlWithoutEndpoints.Id
	);

	public static bool IsAnalyzerOwned(DiagnosticDescriptor descriptor) => AnalyzerOwnedRuleIds.Contains(descriptor.Id);

	public static bool IsExecutionOnly(DiagnosticDescriptor descriptor) => ExecutionOnlyRuleIds.Contains(descriptor.Id);

	public static ImmutableArray<RuleEvaluation> EvaluateHostKit(
		INamedTypeSymbol symbol,
		CancellationToken cancellationToken
	)
	{
		var diagnostics = ImmutableArray.CreateBuilder<RuleEvaluation>();
		var declaration = GetClassDeclaration(symbol, cancellationToken);
		if (declaration is null)
			return diagnostics.ToImmutable();

		if (!TypeHelpers.IsPartial(declaration))
		{
			diagnostics.Add(
				new(
					DiagnosticLibrary.ClassMustBePartial,
					IsBlocking: true,
					declaration.Identifier.GetLocation(),
					[symbol.Name]
				)
			);
		}

		if (TypeHelpers.HasNonEmptyConstructors(declaration, symbol.Name))
		{
			diagnostics.Add(
				new(
					DiagnosticLibrary.NonEmptyConstructorsNotSupported,
					IsBlocking: true,
					declaration.Identifier.GetLocation(),
					[symbol.Name]
				)
			);
		}

		return diagnostics.ToImmutable();
	}

	public static ImmutableArray<RuleEvaluation> EvaluateResourceKit(
		INamedTypeSymbol symbol,
		Compilation compilation,
		CancellationToken cancellationToken
	)
	{
		var diagnostics = ImmutableArray.CreateBuilder<RuleEvaluation>();
		var declaration = GetClassDeclaration(symbol, cancellationToken);

		if (declaration is null)
			return diagnostics.ToImmutable();

		if (!TypeHelpers.IsPartial(declaration))
		{
			diagnostics.Add(
				new(
					DiagnosticLibrary.ClassMustBePartial,
					IsBlocking: true,
					declaration.Identifier.GetLocation(),
					[symbol.Name]
				)
			);
		}

		if (TypeHelpers.HasNonEmptyConstructors(declaration, symbol.Name))
		{
			diagnostics.Add(
				new(
					DiagnosticLibrary.NonEmptyConstructorsNotSupported,
					IsBlocking: true,
					declaration.Identifier.GetLocation(),
					[symbol.Name]
				)
			);
		}

		var allAttributes = ResourceDefinitionAttributeData.AllAttributeData(symbol).ToArray();
		var matchedAttribute = allAttributes.FirstOrDefault();
		if (matchedAttribute.Attribute is null)
			return diagnostics.ToImmutable();

		var resourceName = matchedAttribute.Instance.Name ?? symbol.Name.TrimSuffix(TypeLibraryGenerator.TrimSuffixes);
		// The analyzer runs after generation, so the generated ResourceKitBase<TResource> already exists as
		// the symbol's base type. "Explicit base" must therefore be judged from the source declaration, not
		// the resolved symbol, so generic definitions without a declared base are not mistaken for explicit
		// base classes.
		var hasExplicitBaseType = TypeHelpers.HasExplicitBaseType(declaration);
		var isDerivedFromExpectedBase =
			hasExplicitBaseType
			&& TypeHelpers.IsDerivedFromExpectedBase(symbol, TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitBase);
		var isGenericResourceDefinition = matchedAttribute.Attribute.AttributeClass!.IsGenericType;
		var propertyName = matchedAttribute.Instance.PropertyName
			?? symbol.Name.TrimSuffix(TypeLibraryGenerator.TrimSuffixes)!;
		var (_, isValidResourceType, declaredProject) = ResolveResourceType(matchedAttribute, symbol);

		if (allAttributes.Length > 1)
		{
			diagnostics.Add(
				new(
					DiagnosticLibrary.MixedResourceDefinitionAttributesNotSupported,
					IsBlocking: true,
					GetLocation(symbol),
					[symbol.Name]
				)
			);
		}

		if (isGenericResourceDefinition && hasExplicitBaseType)
		{
			diagnostics.Add(
				new(
					DiagnosticLibrary.GenericResourceDefinitionCannotHaveExplicitBase,
					IsBlocking: true,
					GetLocation(symbol),
					[symbol.Name]
				)
			);
		}
		else if (!isGenericResourceDefinition && !hasExplicitBaseType)
		{
			diagnostics.Add(
				new(
					DiagnosticLibrary.NonGenericResourceDefinitionRequiresExplicitBase,
					IsBlocking: true,
					GetLocation(symbol),
					[symbol.Name, TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitBase.MetadataFullName]
				)
			);
		}

		if (string.IsNullOrWhiteSpace(resourceName))
		{
			diagnostics.Add(
				new(DiagnosticLibrary.ResourceNameNotDerivable, IsBlocking: true, GetLocation(symbol), [symbol.Name])
			);
		}

		if (!TypeHelpers.IsValidIdentifier(propertyName))
		{
			diagnostics.Add(
				new(DiagnosticLibrary.InvalidPropertyName, IsBlocking: true, GetLocation(symbol), [propertyName])
			);
		}

		if (hasExplicitBaseType && !isDerivedFromExpectedBase)
		{
			diagnostics.Add(
				new(
					DiagnosticLibrary.ResourceMustDeriveFromResourceKitBase,
					IsBlocking: true,
					GetLocation(symbol),
					[symbol.Name]
				)
			);
		}

		if (!isValidResourceType)
		{
			diagnostics.Add(new(DiagnosticLibrary.NoAspireResourceFound, IsBlocking: true, GetLocation(symbol), []));
		}

		if (declaredProject is not null)
		{
			var (addsDeclaredProject, addProjectLocation) = BuildResourceAddsProject(
				symbol,
				declaredProject,
				compilation,
				cancellationToken
			);
			if (!addsDeclaredProject)
			{
				diagnostics.Add(
					new(
						DiagnosticLibrary.ProjectDefinitionMismatch,
						IsBlocking: false,
						addProjectLocation ?? GetLocation(symbol),
						[symbol.Name, declaredProject.Name]
					)
				);
			}

			if (hasExplicitBaseType)
			{
				var baseResourceType = ResolveAspireResourceTypeFromBaseClass(symbol, TypeIdentity.Empty);
				if (
					baseResourceType != TypeIdentity.Empty
					&& baseResourceType != TypeLibrary.Aspire.Hosting.ApplicationModel.ProjectResource
				)
				{
					diagnostics.Add(
						new(
							DiagnosticLibrary.ProjectResourceKitBaseMismatch,
							IsBlocking: false,
							GetLocation(symbol),
							[symbol.Name, baseResourceType.MetadataFullName]
						)
					);
				}
			}
		}

		EvaluateResourceBuilderProperties(symbol, compilation, diagnostics, cancellationToken);
		EvaluateGroupMemberships(symbol, diagnostics, cancellationToken);
		EvaluateUrls(
			symbol,
			ResourceHasEndpoints(matchedAttribute, symbol),
			isValidResourceType ? DisplayName(matchedAttribute, symbol) : null,
			diagnostics,
			cancellationToken
		);

		return diagnostics.ToImmutable();
	}

	public static ImmutableArray<RuleEvaluation> EvaluateResourceGroup(
		INamedTypeSymbol symbol,
		CancellationToken cancellationToken
	)
	{
		var diagnostics = ImmutableArray.CreateBuilder<RuleEvaluation>();
		var declaration = GetClassDeclaration(symbol, cancellationToken);
		if (declaration is null)
			return diagnostics.ToImmutable();

		if (!TypeHelpers.IsPartial(declaration))
		{
			diagnostics.Add(
				new(
					DiagnosticLibrary.ClassMustBePartial,
					IsBlocking: true,
					declaration.Identifier.GetLocation(),
					[symbol.Name]
				)
			);
		}

		if (TypeHelpers.HasNonEmptyConstructors(declaration, symbol.Name))
		{
			diagnostics.Add(
				new(
					DiagnosticLibrary.NonEmptyConstructorsNotSupported,
					IsBlocking: true,
					declaration.Identifier.GetLocation(),
					[symbol.Name]
				)
			);
		}

		// As for resource kits, judge the base from the source: the generated base already exists here.
		if (TypeHelpers.HasExplicitBaseType(declaration))
		{
			diagnostics.Add(
				new(
					DiagnosticLibrary.ResourceGroupCannotHaveExplicitBase,
					IsBlocking: true,
					declaration.Identifier.GetLocation(),
					[symbol.Name]
				)
			);
		}

		var propertyName = GroupPropertyName(symbol);
		if (!TypeHelpers.IsValidIdentifier(propertyName))
		{
			diagnostics.Add(
				new(DiagnosticLibrary.InvalidPropertyName, IsBlocking: true, GetLocation(symbol), [propertyName])
			);
		}

		if (IsGenericOrInGenericType(symbol))
		{
			diagnostics.Add(
				new(
					DiagnosticLibrary.ResourceGroupCannotBeGeneric,
					IsBlocking: true,
					declaration.Identifier.GetLocation(),
					[symbol.Name]
				)
			);
		}

		// A group is never a member of anything, itself included: groups nest through their parent only.
		if (
			TypeHelpers.HasAttribute(symbol, TypeLibrary.Purview.Aspire.ResourceKit.ResourceDefinitionAttribute)
			|| TypeHelpers.HasAttribute(
				symbol,
				TypeLibrary.Purview.Aspire.ResourceKit.GenericResourceDefinitionAttribute
			)
			|| InGroupAttributeData.AllAttributeData(symbol).Any()
		)
		{
			diagnostics.Add(
				new(
					DiagnosticLibrary.ResourceGroupCannotBeResourceKit,
					IsBlocking: true,
					declaration.Identifier.GetLocation(),
					[symbol.Name]
				)
			);
		}

		var groupAttributeCount = symbol.GetAttributes().Count(IsResourceGroupAttribute);
		var parentDeclarations = GetGroupParentDeclarations(symbol);
		if (groupAttributeCount > 1 || parentDeclarations.Length > 1)
		{
			diagnostics.Add(
				new(
					DiagnosticLibrary.MultipleResourceGroupParents,
					IsBlocking: true,
					GetLocation(symbol),
					[symbol.Name]
				)
			);
		}

		var parent = parentDeclarations.FirstOrDefault();
		if (parent is not null)
		{
			if (!IsResourceGroup(parent))
			{
				diagnostics.Add(
					new(
						DiagnosticLibrary.GroupTypeNotResourceGroup,
						IsBlocking: true,
						GetLocation(symbol),
						[parent.ToDisplayString(), symbol.Name]
					)
				);
			}
			else if (IsInParentCycle(symbol))
			{
				diagnostics.Add(
					new(DiagnosticLibrary.ResourceGroupCycle, IsBlocking: true, GetLocation(symbol), [symbol.Name])
				);
			}
		}

		EvaluateUrls(
			symbol,
			hasEndpoints: false,
			TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitGroupResource.Name,
			diagnostics,
			cancellationToken
		);

		return diagnostics.ToImmutable();
	}

	/// <summary>
	/// Reads the groups a resource kit belongs to, primary group first, each once.
	/// </summary>
	public static ImmutableArray<INamedTypeSymbol> GetGroups(INamedTypeSymbol symbol)
	{
		var memberships = InGroupAttributeData
			.AllAttributeData(symbol)
			.Select(static m =>
				(
					m.Instance.Primary,
					Group: m.Attribute.AttributeClass is { TypeArguments.Length: 1 } attributeClass
						? attributeClass.TypeArguments[0] as INamedTypeSymbol
						: null
				)
			)
			.Where(static m => m.Group is not null)
			.ToArray();

		// A stable sort: the first Primary group moves to the front, everything else keeps declaration order.
		return
		[
			.. memberships
				.OrderBy(static m => m.Primary ? 0 : 1)
				.Select(static m => m.Group!)
				.Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default),
		];
	}

	/// <summary>
	/// Reads the parent of a resource group: the <c>Parent</c> named argument, which is a <see cref="Type"/> and
	/// so cannot be held by the attribute-data model, or the type argument of <c>ResourceGroup&lt;TParent&gt;</c>.
	/// </summary>
	/// <remarks>
	/// The first declaration wins; more than one is <c>SG0029</c>, which blocks generation.
	/// </remarks>
	public static INamedTypeSymbol? GetGroupParent(INamedTypeSymbol symbol) =>
		GetGroupParentDeclarations(symbol).FirstOrDefault();

	/// <summary>
	/// Reads every parent a resource group declares, in declaration order.
	/// </summary>
	public static ImmutableArray<INamedTypeSymbol> GetGroupParentDeclarations(INamedTypeSymbol symbol)
	{
		var parents = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
		foreach (var attribute in symbol.GetAttributes().Where(IsResourceGroupAttribute))
		{
			if (
				attribute.AttributeClass is { IsGenericType: true, TypeArguments.Length: 1 } attributeClass
				&& attributeClass.TypeArguments[0] is INamedTypeSymbol typeArgument
			)
			{
				parents.Add(typeArgument);
			}

			foreach (var argument in attribute.NamedArguments)
			{
				if (argument.Key == "Parent" && argument.Value.Value is INamedTypeSymbol parent)
					parents.Add(parent);
			}
		}

		return parents.ToImmutable();
	}

	/// <summary>
	/// Determines whether an attribute application is <c>[ResourceGroup]</c> or <c>[ResourceGroup&lt;TParent&gt;]</c>.
	/// </summary>
	public static bool IsResourceGroupAttribute(AttributeData attribute) =>
		attribute.AttributeClass is { } attributeClass
		&& (
			TypeLibrary.Purview.Aspire.ResourceKit.ResourceGroupAttribute.Matches(attributeClass)
			|| TypeLibrary.Purview.Aspire.ResourceKit.GenericResourceGroupAttribute.Matches(
				attributeClass.OriginalDefinition
			)
		);

	public static bool HasResourceGroupAttribute(INamedTypeSymbol symbol) =>
		symbol.GetAttributes().Any(IsResourceGroupAttribute);

	public static bool IsResourceGroup(INamedTypeSymbol symbol) =>
		symbol.DeclaringSyntaxReferences.Length > 0 && HasResourceGroupAttribute(symbol);

	static bool IsGenericOrInGenericType(INamedTypeSymbol symbol)
	{
		for (var type = symbol; type is not null; type = type.ContainingType)
		{
			if (type.IsGenericType)
				return true;
		}

		return false;
	}

	public static string GroupPropertyName(INamedTypeSymbol symbol) =>
		symbol.Name.TrimSuffix(TypeLibraryGenerator.GroupTrimSuffixes) ?? symbol.Name;

	public static string GroupName(INamedTypeSymbol symbol) =>
		ResourceGroupAttributeData.FromAttributeData(symbol.GetAttributes()).Name ?? GroupPropertyName(symbol);

	/// <summary>
	/// Determines whether a group's parent chain leads back to the group itself.
	/// </summary>
	/// <remarks>
	/// The walk always terminates. Each step is normalised to its <see cref="ITypeSymbol.OriginalDefinition"/>,
	/// so a constructed generic such as <c>G&lt;G&lt;T&gt;&gt;</c> cannot produce an unbounded supply of new types,
	/// and a step that reaches an already-visited type stops. There are finitely many declared types, so the
	/// walk takes at most one step per declared group.
	/// </remarks>
	internal static bool IsInParentCycle(INamedTypeSymbol symbol)
	{
		var start = symbol.OriginalDefinition;
		HashSet<INamedTypeSymbol> seen = new(SymbolEqualityComparer.Default) { start };
		for (
			var parent = GetGroupParent(start)?.OriginalDefinition;
			parent is not null;
			parent = GetGroupParent(parent)?.OriginalDefinition
		)
		{
			if (SymbolEqualityComparer.Default.Equals(parent, start))
				return true;

			// A cycle further up the chain is reported on the groups that form it.
			if (!seen.Add(parent))
				return false;
		}

		return false;
	}

	static void EvaluateGroupMemberships(
		INamedTypeSymbol symbol,
		ImmutableArray<RuleEvaluation>.Builder diagnostics,
		CancellationToken cancellationToken
	)
	{
		var memberships = InGroupAttributeData.AllAttributeData(symbol).ToArray();
		foreach (var (_, attribute) in memberships)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (
				attribute.AttributeClass is { TypeArguments.Length: 1 } attributeClass
				&& attributeClass.TypeArguments[0] is INamedTypeSymbol group
				&& !IsResourceGroup(group)
			)
			{
				diagnostics.Add(
					new(
						DiagnosticLibrary.GroupTypeNotResourceGroup,
						IsBlocking: true,
						GetLocation(attribute, cancellationToken) ?? GetLocation(symbol),
						[group.ToDisplayString(), symbol.Name]
					)
				);
			}
		}

		var primaries = memberships.Where(static m => m.Instance.Primary).ToArray();
		if (primaries.Length > 1)
		{
			diagnostics.Add(
				new(
					DiagnosticLibrary.MultiplePrimaryGroups,
					IsBlocking: false,
					GetLocation(primaries[1].Attribute, cancellationToken) ?? GetLocation(symbol),
					[symbol.Name, primaries[0].Instance.GroupType.Name]
				)
			);
		}
	}

	/// <summary>
	/// Reads the <c>[ResourceUrl]</c> and <c>[EndpointUrl]</c> applications on a resource kit or group, in
	/// declaration order.
	/// </summary>
	public static ImmutableArray<(UrlModel Model, AttributeData Attribute)> GetUrls(INamedTypeSymbol symbol)
	{
		var urls = ImmutableArray.CreateBuilder<(UrlModel, AttributeData)>();
		foreach (var attribute in symbol.GetAttributes())
		{
			if (attribute.AttributeClass is null)
				continue;

			if (TypeLibrary.Purview.Aspire.ResourceKit.ResourceUrlAttribute.Matches(attribute.AttributeClass))
			{
				var data = ResourceUrlAttributeData.FromAttributeData(attribute);
				urls.Add(
					(
						new(data.Name ?? string.Empty, data.Url, data.DisplayText, data.Endpoint, data.DisplayLocation),
						attribute
					)
				);
			}
			else if (TypeLibrary.Purview.Aspire.ResourceKit.EndpointUrlAttribute.Matches(attribute.AttributeClass))
			{
				var data = EndpointUrlAttributeData.FromAttributeData(attribute);
				urls.Add(
					(
						new(
							EndpointPropertyName(data.Endpoint ?? string.Empty),
							Url: null,
							data.DisplayText,
							data.Endpoint,
							data.DisplayLocation
						),
						attribute
					)
				);
			}
		}

		return urls.ToImmutable();
	}

	// Endpoint names are often lower case and hyphenated ("otlp-grpc"); the options member is "OtlpGrpc".
	static string EndpointPropertyName(string endpoint)
	{
		System.Text.StringBuilder builder = new(endpoint.Length);
		var upperNext = true;
		foreach (var c in endpoint)
		{
			if (!char.IsLetterOrDigit(c))
			{
				upperNext = true;
				continue;
			}

			builder.Append(upperNext ? char.ToUpperInvariant(c) : c);
			upperNext = false;
		}

		return builder.ToString();
	}

	static void EvaluateUrls(
		INamedTypeSymbol symbol,
		bool? hasEndpoints,
		string? resourceTypeName,
		ImmutableArray<RuleEvaluation>.Builder diagnostics,
		CancellationToken cancellationToken
	)
	{
		HashSet<string> seen = new(StringComparer.Ordinal);
		foreach (var (url, attribute) in GetUrls(symbol))
		{
			cancellationToken.ThrowIfCancellationRequested();

			var location = GetLocation(attribute, cancellationToken) ?? GetLocation(symbol);
			if (!TypeHelpers.IsValidIdentifier(url.PropertyName))
			{
				diagnostics.Add(
					new(DiagnosticLibrary.InvalidPropertyName, IsBlocking: true, location, [url.PropertyName])
				);
			}
			else if (!seen.Add(url.PropertyName))
			{
				diagnostics.Add(
					new(DiagnosticLibrary.DuplicateUrlName, IsBlocking: true, location, [symbol.Name, url.PropertyName])
				);
			}

			// Only report against a resource type we could resolve: an unresolved one already reports SG0016.
			if (hasEndpoints != false || resourceTypeName is null)
				continue;

			if (url.Endpoint is not null)
			{
				diagnostics.Add(
					new(
						DiagnosticLibrary.EndpointUrlRequiresEndpoints,
						IsBlocking: true,
						location,
						[symbol.Name, url.Endpoint, resourceTypeName]
					)
				);
			}
			else if (url.Url is not null && !IsAbsoluteUrl(url.Url))
			{
				diagnostics.Add(
					new(
						DiagnosticLibrary.RelativeUrlWithoutEndpoints,
						IsBlocking: false,
						location,
						[symbol.Name, url.Url, resourceTypeName]
					)
				);
			}
		}
	}

	// Mirrors ResourceKitUrl.IsAbsolute: "/health" parses as a file URI on Unix, so require a non-file URI.
	static bool IsAbsoluteUrl(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) && !uri.IsFile;

	/// <summary>
	/// Determines whether the resource a kit builds has endpoints, or <see langword="null"/> when the resource
	/// type cannot be resolved.
	/// </summary>
	static bool? ResourceHasEndpoints(
		(ResourceDefinitionAttributeData Instance, AttributeData Attribute) matchedAttribute,
		INamedTypeSymbol kitSymbol
	)
	{
		var resourceType = ResolveResourceTypeSymbol(matchedAttribute, kitSymbol);
		if (resourceType is null)
			return null;

		if (TypeHelpers.Implements(resourceType, TypeLibrary.Aspire.Hosting.IProjectMetadata))
			return true;

		return TypeHelpers.Implements(resourceType, TypeLibrary.Aspire.Hosting.ApplicationModel.IResourceWithEndpoints);
	}

	static string? DisplayName(
		(ResourceDefinitionAttributeData Instance, AttributeData Attribute) matchedAttribute,
		INamedTypeSymbol kitSymbol
	) => ResolveResourceTypeSymbol(matchedAttribute, kitSymbol)?.Name;

	static INamedTypeSymbol? ResolveResourceTypeSymbol(
		(ResourceDefinitionAttributeData Instance, AttributeData Attribute) matchedAttribute,
		INamedTypeSymbol kitSymbol
	)
	{
		if (
			matchedAttribute.Attribute?.AttributeClass is
			{ IsGenericType: true, TypeArguments.Length: > 0 } attributeClass
		)
			return attributeClass.TypeArguments[0] as INamedTypeSymbol;

		if (kitSymbol.BaseType is null)
			return null;

		foreach (var argument in kitSymbol.BaseType.TypeArguments)
		{
			if (
				argument is INamedTypeSymbol named
				&& TypeHelpers.Implements(named, TypeLibrary.Aspire.Hosting.ApplicationModel.IResource)
			)
				return named;
		}

		return null;
	}

	static Location? GetLocation(AttributeData attribute, CancellationToken cancellationToken) =>
		attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation();

	/// <summary>
	/// Evaluates <c>SG0017</c> for every <c>IResourceBuilder&lt;T&gt;</c> property that is never assigned in
	/// <c>BuildResource</c> or <c>ConfigureResource</c>.
	/// </summary>
	static void EvaluateResourceBuilderProperties(
		INamedTypeSymbol symbol,
		Compilation compilation,
		ImmutableArray<RuleEvaluation>.Builder diagnostics,
		CancellationToken cancellationToken
	)
	{
		var iResourceBuilder = TypeLibrary.Aspire.Hosting.ApplicationModel.IResourceBuilder;
		foreach (var member in symbol.GetMembers())
		{
			if (member is not IPropertySymbol property)
				continue;

			if (!iResourceBuilder.Matches(property.Type))
				continue;

			if (IsAssignedInLifecycleMethods(symbol, property, compilation, cancellationToken))
				continue;

			diagnostics.Add(
				new(
					DiagnosticLibrary.ResourcePropertyNeverSet,
					IsBlocking: false,
					property.Locations.FirstOrDefault(static location => location.IsInSource),
					[property.Name, property.Type.ToDisplayString()]
				)
			);
		}
	}

	static bool IsAssignedInLifecycleMethods(
		INamedTypeSymbol type,
		IPropertySymbol property,
		Compilation compilation,
		CancellationToken cancellationToken
	)
	{
		foreach (var methodName in new[] { "BuildResource", "ConfigureResource" })
		{
			foreach (var method in type.GetMembers(methodName).OfType<IMethodSymbol>())
			{
				foreach (var reference in method.DeclaringSyntaxReferences)
				{
					var syntax = reference.GetSyntax(cancellationToken);
					var model = compilation.GetSemanticModel(syntax.SyntaxTree);
					foreach (var assignment in syntax.DescendantNodes().OfType<AssignmentExpressionSyntax>())
					{
						var target = model.GetSymbolInfo(assignment.Left, cancellationToken).Symbol;
						if (SymbolEqualityComparer.Default.Equals(target, property))
							return true;
					}
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Resolves the resource type for a resource kit. A generic type argument that implements
	/// <c>IResource</c> is used as-is; one that implements <c>IProjectMetadata</c> (the type accepted by
	/// <c>AddProject&lt;TProject&gt;</c>) maps to <c>ProjectResource</c>. Anything else is invalid.
	/// </summary>
	public static (
		TypeIdentity AspireResourceType,
		bool IsValid,
		INamedTypeSymbol? DeclaredProject
	) ResolveResourceType(
		(ResourceDefinitionAttributeData Instance, AttributeData Attribute) matchedAttribute,
		INamedTypeSymbol kitSymbol
	)
	{
		var isGeneric = matchedAttribute.Attribute.AttributeClass?.IsGenericType == true;
		var aspireResourceType = matchedAttribute.Instance.AspireResourceType;

		if (
			isGeneric
			&& matchedAttribute.Attribute.AttributeClass is { TypeArguments.Length: > 0 } attributeClass
			&& attributeClass.TypeArguments[0] is INamedTypeSymbol typeArgument
		)
		{
			if (TypeHelpers.Implements(typeArgument, TypeLibrary.Aspire.Hosting.ApplicationModel.IResource))
				return (aspireResourceType, true, null);

			if (TypeHelpers.Implements(typeArgument, TypeLibrary.Aspire.Hosting.IProjectMetadata))
				return (TypeLibrary.Aspire.Hosting.ApplicationModel.ProjectResource, true, typeArgument);

			return (aspireResourceType, false, null);
		}

		if (aspireResourceType == TypeIdentity.Empty)
			aspireResourceType = ResolveAspireResourceTypeFromBaseClass(kitSymbol, aspireResourceType);

		return (aspireResourceType, aspireResourceType != TypeIdentity.Empty, null);
	}

	/// <summary>
	/// Determines whether <c>BuildResource</c> adds the declared project via <c>AddProject&lt;TProject&gt;</c>,
	/// reporting the first <c>AddProject</c> invocation found so the diagnostic can point at it.
	/// </summary>
	static (bool AddsDeclaredProject, Location? AddProjectLocation) BuildResourceAddsProject(
		INamedTypeSymbol type,
		INamedTypeSymbol declaredProject,
		Compilation compilation,
		CancellationToken cancellationToken
	)
	{
		Location? addProjectLocation = null;
		foreach (var method in type.GetMembers("BuildResource").OfType<IMethodSymbol>())
		{
			foreach (var reference in method.DeclaringSyntaxReferences)
			{
				var syntax = reference.GetSyntax(cancellationToken);
				var model = compilation.GetSemanticModel(syntax.SyntaxTree);
				foreach (var invocation in syntax.DescendantNodes().OfType<InvocationExpressionSyntax>())
				{
					if (
						invocation.Expression
						is not MemberAccessExpressionSyntax
						{
							Name: GenericNameSyntax
							{
								Identifier.ValueText: "AddProject",
								TypeArgumentList.Arguments.Count: 1,
							} genericName
						}
					)
						continue;

					addProjectLocation ??= invocation.GetLocation();
					if (
						model.GetTypeInfo(genericName.TypeArgumentList.Arguments[0], cancellationToken).Type
							is INamedTypeSymbol projectType
						&& SymbolEqualityComparer.Default.Equals(projectType, declaredProject)
					)
						return (true, null);
				}
			}
		}

		return (false, addProjectLocation);
	}

	public static TypeIdentity ResolveAspireResourceTypeFromBaseClass(
		INamedTypeSymbol symbol,
		TypeIdentity aspireResourceType
	)
	{
		if (symbol.BaseType is null || symbol.BaseType.TypeParameters.Length == 0)
			return aspireResourceType;

		foreach (var param in symbol.BaseType.TypeArguments)
		{
			foreach (var @interface in param.AllInterfaces)
			{
				TypeIdentity t = new(@interface);
				if (t == TypeLibrary.Aspire.Hosting.ApplicationModel.IResource)
					return new(param);
			}
		}

		return aspireResourceType;
	}

	static ClassDeclarationSyntax? GetClassDeclaration(INamedTypeSymbol symbol, CancellationToken cancellationToken)
	{
		foreach (var reference in symbol.DeclaringSyntaxReferences)
		{
			if (reference.GetSyntax(cancellationToken) is ClassDeclarationSyntax declaration)
				return declaration;
		}

		return null;
	}

	static Location? GetLocation(ISymbol symbol) =>
		symbol.Locations.FirstOrDefault(static location => location.IsInSource);
}

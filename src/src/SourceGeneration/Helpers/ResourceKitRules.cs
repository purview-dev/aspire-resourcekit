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
		DiagnosticLibrary.ProjectResourceKitBaseMismatch.Id
	);

	/// <summary>
	/// A neutral rule evaluation that can be converted into either a <see cref="SourceGeneratorFramework.DiagnosticInfo"/> (for the
	/// generator's incremental model) or a Roslyn <see cref="Diagnostic"/> (for the analyzer).
	/// </summary>
	internal readonly record struct RuleEvaluation(
		DiagnosticDescriptor Descriptor,
		Location? Location,
		ImmutableArray<object> MessageArgs
	)
	{
		public DiagnosticInfo ToDiagnosticInfo() => DiagnosticInfo.Create(Descriptor, Location, [.. MessageArgs]);

		public Diagnostic ToDiagnostic() => Diagnostic.Create(Descriptor, Location ?? Location.None, [.. MessageArgs]);
	}

	public static bool IsAnalyzerOwned(DiagnosticDescriptor descriptor) => AnalyzerOwnedRuleIds.Contains(descriptor.Id);

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
				new(DiagnosticLibrary.ClassMustBePartial, declaration.Identifier.GetLocation(), [symbol.Name])
			);
		}

		if (TypeHelpers.HasNonEmptyConstructors(declaration, symbol.Name))
		{
			diagnostics.Add(
				new(
					DiagnosticLibrary.NonEmptyConstructorsNotSupported,
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
				new(DiagnosticLibrary.ClassMustBePartial, declaration.Identifier.GetLocation(), [symbol.Name])
			);
		}

		if (TypeHelpers.HasNonEmptyConstructors(declaration, symbol.Name))
		{
			diagnostics.Add(
				new(
					DiagnosticLibrary.NonEmptyConstructorsNotSupported,
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
		var propertyName =
			matchedAttribute.Instance.PropertyName ?? symbol.Name.TrimSuffix(TypeLibraryGenerator.TrimSuffixes)!;
		var (_, isValidResourceType, declaredProject) = ResolveResourceType(matchedAttribute, symbol);

		if (allAttributes.Length > 1)
		{
			diagnostics.Add(
				new(DiagnosticLibrary.MixedResourceDefinitionAttributesNotSupported, GetLocation(symbol), [symbol.Name])
			);
		}

		if (isGenericResourceDefinition && hasExplicitBaseType)
		{
			diagnostics.Add(
				new(
					DiagnosticLibrary.GenericResourceDefinitionCannotHaveExplicitBase,
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
					GetLocation(symbol),
					[symbol.Name, TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitBase.MetadataFullName]
				)
			);
		}

		if (string.IsNullOrWhiteSpace(resourceName))
		{
			diagnostics.Add(new(DiagnosticLibrary.ResourceNameNotDerivable, GetLocation(symbol), [symbol.Name]));
		}

		if (!TypeHelpers.IsValidIdentifier(propertyName))
		{
			diagnostics.Add(new(DiagnosticLibrary.InvalidPropertyName, GetLocation(symbol), [propertyName]));
		}

		if (hasExplicitBaseType && !isDerivedFromExpectedBase)
		{
			diagnostics.Add(
				new(DiagnosticLibrary.ResourceMustDeriveFromResourceKitBase, GetLocation(symbol), [symbol.Name])
			);
		}

		if (!isValidResourceType)
		{
			diagnostics.Add(new(DiagnosticLibrary.NoAspireResourceFound, GetLocation(symbol), []));
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
							GetLocation(symbol),
							[symbol.Name, baseResourceType.MetadataFullName]
						)
					);
				}
			}
		}

		EvaluateResourceBuilderProperties(symbol, compilation, diagnostics, cancellationToken);

		return diagnostics.ToImmutable();
	}

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
				var t = new TypeIdentity(@interface);
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

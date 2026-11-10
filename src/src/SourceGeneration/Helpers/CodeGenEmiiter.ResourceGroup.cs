using Purview.Aspire.ResourceKit.SourceGeneration.Models;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

partial class CodeGenEmiiter
{
	const string GroupsClassName = "ResourceKitGroups";
	const string GroupsOptionsClassName = "ResourceKitGroupsOptions";

	static void EmitResourceGroups(OutputContext context, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (!context.HasResourceGroups)
			return;

		context.Debug($"Emitting resource groups for host kit: {context.HostKit.HostKitType}");

		var groupBase = TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitGroup.MakeGeneric(
			context.HostKit.HostKitType
		);

		foreach (var group in context.ResourceGroups)
		{
			cancellationToken.ThrowIfCancellationRequested();

			context.Debug($"Processing resource group: {group.GroupType.Name}", 1);

			using var groupNs = context.Writer.BlockNamespaceScope(group.GroupType);
			using (
				context
					.Writer.XmlSummary($"Represents the '{group.Name}' resource group.")
					.ClassScope(new(group.GroupType, group.Accessibility) { IsPartial = true, BaseType = groupBase })
			)
			{
				context
					.Writer.XmlSummary("Initializes a new instance of the resource group.")
					.Constructor(
						new(group.GroupType, TypeDeclarationAccessibility.Public)
						{
							Parameters = context.HostKit.ShouldGenerateOptions
								?
								[
									new("options", group.OptionsType),
									new("parent", groupBase.MakeNullable(context.Writer)),
								]
								: [new("parent", groupBase.MakeNullable(context.Writer))],
							Initializer = $"base({group.Name.StringLiteral()}, parent)",
						},
						body =>
						{
							if (context.HostKit.ShouldGenerateOptions)
							{
								body.Assignment(
										"Options",
										$"options ?? throw new {TypeLibrary.System.ArgumentNullException}(nameof(options))"
									)
									.Assignment("IsEnabled", "options.IsEnabled")
									.Assignment("Mode", "options.Mode")
									.Assignment("ShowInDashboard", "options.ShowInDashboard");
							}
							else
							{
								body.Assignment("Mode", ModeExpression(group.Mode))
									.Assignment("ShowInDashboard", group.ShowInDashboard ? "true" : "false");
							}

							if (!group.Urls.IsEmpty)
								body.Assignment(
									"Urls",
									UrlsExpression(group.Urls, context.HostKit.ShouldGenerateOptions)
								);
						}
					);

				if (context.HostKit.ShouldGenerateOptions)
				{
					context
						.Writer.XmlSummary("Gets the resource group options.")
						.Property("Options", group.OptionsType, TypeDeclarationAccessibility.Public);

					GenerateResourceGroupOptionsClass(context, group, cancellationToken);
				}
			}
		}
	}

	static void GenerateResourceGroupOptionsClass(
		OutputContext context,
		ResourceGroupModel group,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		context.Debug($"Generating Resource Group Options class: {group.OptionsType.Name}", 2);

		context.Writer.XmlSummary(
			$"Typed settings for <see cref=\"{group.GroupType}\" />.",
			$"Can be accessed by resolving <see cref=\"{context.HostKit.OptionsType}.Groups\" />."
		);

		using (
			context.Writer.ClassScope(
				new(group.OptionsType, TypeDeclarationAccessibility.Public) { IsSealed = true, IsPartial = true }
			)
		)
		{
			context
				.Writer.XmlSummary("Gets or sets whether the group, and so every resource in it, is enabled.")
				.Property(
					new("IsEnabled", TypeLibrary.System.Boolean, TypeDeclarationAccessibility.Public)
					{
						HasSetter = true,
						Initializer = "true",
					}
				);

			context
				.Writer.NewLine()
				.XmlSummary(
					"Gets or sets the execution mode in which the group, and so every resource in it, participates."
				)
				.Property(
					new(
						"Mode",
						TypeLibrary.Purview.Aspire.ResourceKit.ResourceKitMode,
						TypeDeclarationAccessibility.Public
					)
					{
						HasSetter = true,
						Initializer = ModeExpression(group.Mode),
					}
				);

			context
				.Writer.NewLine()
				.XmlSummary("Gets or sets whether the group is shown as a node in the dashboard.")
				.Property(
					new("ShowInDashboard", TypeLibrary.System.Boolean, TypeDeclarationAccessibility.Public)
					{
						HasSetter = true,
						Initializer = group.ShowInDashboard ? "true" : "false",
					}
				);

			if (!group.Urls.IsEmpty)
			{
				context
					.Writer.NewLine()
					.XmlSummary("Gets or sets the dashboard URLs declared for the group.")
					.Property(
						new("Urls", group.OptionsType.Nested("UrlsOptions"), TypeDeclarationAccessibility.Public)
						{
							HasSetter = true,
							Initializer = "new()",
						}
					);

				GenerateUrlsOptionsClass(context.Writer, group.Urls);
			}
		}
	}

	// The host kit's typed group accessor: one property per group, created parents first so a child can be
	// handed its parent.
	static void GenerateGroupsClass(OutputContext context, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		context.Writer.NewLine().XmlSummary("Provides typed access to the host kit's resource groups.");

		using (context.Writer.ClassScope(new(GroupsClassName, TypeDeclarationAccessibility.Public) { IsSealed = true }))
		{
			context
				.Writer.XmlSummary("Initializes the resource groups, parents before their children.")
				.Constructor(
					new(context.HostKit.HostKitType.Nested(GroupsClassName), TypeDeclarationAccessibility.Internal)
					{
						Parameters = context.HostKit.ShouldGenerateOptions
							? [new("options", context.HostKit.OptionsType)]
							: [],
					},
					body =>
					{
						foreach (var group in context.ResourceGroups)
						{
							var parent = group.HasParent ? PropertyNameOf(context, group.ParentType) : "null";
							var arguments = context.HostKit.ShouldGenerateOptions
								? new[] { $"options.Groups.{group.PropertyName}", parent }
								: [parent];

							body.Assignment(group.PropertyName, new ObjectCreationOptions([.. arguments]));
						}
					}
				);

			foreach (var group in context.ResourceGroups)
			{
				cancellationToken.ThrowIfCancellationRequested();

				context
					.Writer.XmlSummary($"Gets the <see cref=\"{group.GroupType}\" /> resource group.")
					.Property(group.PropertyName, group.GroupType, TypeDeclarationAccessibility.Public);
			}
		}
	}

	static void GenerateGroupsOptionsClass(OutputContext context, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		context.Writer.NewLine().XmlSummary("Typed settings for the host kit's resource groups.");

		using (
			context.Writer.ClassScope(
				new(GroupsOptionsClassName, TypeDeclarationAccessibility.Public) { IsSealed = true, IsPartial = true }
			)
		)
		{
			foreach (var group in context.ResourceGroups)
			{
				context
					.Writer.XmlSummary($"Gets or sets options for {CodeWriter.XmlSee(group.GroupType)}.")
					.Property(
						new(group.PropertyName, group.OptionsType, TypeDeclarationAccessibility.Public)
						{
							HasGetter = true,
							IsInitOnly = true,
							Initializer = "new()",
						}
					);
			}
		}
	}

	static string PropertyNameOf(OutputContext context, TypeIdentity groupType) =>
		context.ResourceGroups.AsImmutableArray().First(g => g.GroupType == groupType).PropertyName;
}

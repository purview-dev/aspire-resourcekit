using Aspire.Hosting;

namespace Purview.Aspire.ResourceKit;

/// <summary>
/// Represents a resource kit component that participates in host kit build and configure stages.
/// </summary>
/// <typeparam name="THostKit">The host kit type.</typeparam>
public interface IResourceKit<THostKit>
	where THostKit : class, IHostKit
{
	/// <summary>
	/// Gets the host kit instance associated with this resource.
	/// </summary>
	THostKit HostKit { get; init; }

	/// <summary>
	/// Gets the resource kit that owns this resource kit, or <see langword="null"/> when this kit is a root.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Parentage is composition ownership: the parent is built before the child, the child can read the
	/// parent's <c>ResourceBuilder</c> while building, and disabling the parent disables every descendant.
	/// It is not a general resource dependency and it does not add an Aspire visual parent relationship.
	/// </para>
	/// <para>
	/// The relationship is fixed when the kit is constructed. Implementations that do not derive from
	/// <see cref="ResourceKitBase{THostKit, TResource}"/> inherit this default and are roots.
	/// </para>
	/// </remarks>
	IResourceKit<THostKit>? ParentKit => null;

	/// <summary>
	/// Gets the logical name of the resource.
	/// </summary>
	string Name { get; init; }

	/// <summary>
	/// Gets or sets a value indicating whether this resource is enabled.
	/// </summary>
	bool IsEnabled { get; set; }

	/// <summary>
	/// Builds this resource into the distributed application builder.
	/// </summary>
	/// <param name="builder">The distributed application builder.</param>
	void Build(IDistributedApplicationBuilder builder);

	/// <summary>
	/// Configures this resource using the resolved host kit instance.
	/// </summary>
	void Configure();
}

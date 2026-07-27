using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting;

namespace Purview.Aspire.ResourceKit;

/// <summary>
/// Represents a resource kit component that participates in host kit build and configure stages.
/// </summary>
/// <typeparam name="THostKit">The host kit type.</typeparam>
public interface IResourceKit<THostKit>
	where THostKit : class
{
	/// <summary>
	/// Gets the host kit instance associated with this resource.
	/// </summary>
	THostKit HostKit { get; init; }

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
	void Build([NotNull] IDistributedApplicationBuilder builder);

	/// <summary>
	/// Configures this resource using the resolved host kit instance.
	/// </summary>
	void Configure();
}

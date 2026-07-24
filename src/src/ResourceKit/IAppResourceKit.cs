using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting;

namespace Purview.Aspire.ResourceKit;

/// <summary>
/// Represents a resource kit component that participates in host application build and configure stages.
/// </summary>
/// <typeparam name="THostApp">The host application type.</typeparam>
public interface IAppResourceKit<in THostApp>
	where THostApp : class
{
	/// <summary>
	/// Gets the logical name of the resource.
	/// </summary>
	string Name { get; }

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
	/// Configures this resource using the resolved host application instance.
	/// </summary>
	/// <param name="app">The host application instance.</param>
	void Configure([NotNull] THostApp app);
}

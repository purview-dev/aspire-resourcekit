using Aspire.Hosting;

namespace Purview.Aspire.ResourceKit;

/// <summary>
/// Defines the host application lifecycle for building and configuring Aspire resources.
/// </summary>
public interface IHostKit
{
	/// <summary>
	/// Builds the host application's resources into the distributed application builder.
	/// </summary>
	/// <param name="builder">The distributed application builder used to register resources.</param>
	void Build(IDistributedApplicationBuilder builder);

	/// <summary>
	/// Configures cross-resource relationships after resources have been built.
	/// </summary>
	void Configure();
}

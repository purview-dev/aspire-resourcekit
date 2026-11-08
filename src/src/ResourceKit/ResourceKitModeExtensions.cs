using Aspire.Hosting;
using Microsoft.Extensions.Hosting;

namespace Purview.Aspire.ResourceKit;

/// <summary>
/// Provides helpers for evaluating a <see cref="ResourceKitMode"/> against the current AppHost execution.
/// </summary>
public static class ResourceKitModeExtensions
{
	/// <summary>
	/// Determines whether the specified <see cref="ResourceKitMode"/> participates in the current AppHost execution.
	/// </summary>
	/// <param name="mode">The configured <see cref="ResourceKitMode"/>.</param>
	/// <param name="builder">
	/// The distributed application builder supplying the execution context and hosting environment.
	/// </param>
	/// <returns><see langword="true"/> when the mode participates; otherwise <see langword="false"/>.</returns>
	/// <remarks>
	/// <see cref="ResourceKitMode.LocalDevelopment"/> requires run mode <em>and</em> the development environment,
	/// while <see cref="ResourceKitMode.Run"/> requires run mode only and therefore also covers local development.
	/// </remarks>
	public static bool IsEnabledFor(this ResourceKitMode mode, IDistributedApplicationBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);

		return mode switch
		{
			ResourceKitMode.All => true,
			ResourceKitMode.LocalDevelopment => builder.ExecutionContext.IsRunMode
				&& builder.Environment.IsDevelopment(),
			ResourceKitMode.Run => builder.ExecutionContext.IsRunMode,
			ResourceKitMode.Publish => builder.ExecutionContext.IsPublishMode,
			_ => false,
		};
	}
}

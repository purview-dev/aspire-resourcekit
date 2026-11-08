namespace Purview.Aspire.ResourceKit;

/// <summary>
/// Selects the AppHost execution mode(s) in which a host kit or resource kit participates.
/// </summary>
/// <remarks>
/// <para>
/// The mode is a configured option, so participation is selected from configuration rather than coded into
/// <c>IsResourceEnabled</c> overrides. The default is <see cref="All"/>.
/// </para>
/// <para>
/// Enablement is still observed through <see cref="IResourceKit{THostKit}.IsEnabled"/>: when a mode excludes a
/// kit, <see cref="ResourceKitBase{THostKit, TResource}.Build"/> sets <c>IsEnabled</c> to
/// <see langword="false"/> so both <c>BuildResource(...)</c> and <c>ConfigureResource()</c> are skipped.
/// </para>
/// </remarks>
public enum ResourceKitMode
{
	/// <summary>
	/// Participates in every mode. This is the default.
	/// </summary>
	All = 0,

	/// <summary>
	/// Participates only when the AppHost runs locally in the development environment, for example a
	/// documentation site or a diagram site that only helps while developing.
	/// </summary>
	LocalDevelopment = 1,

	/// <summary>
	/// Participates only in run mode, regardless of environment.
	/// </summary>
	Run = 2,

	/// <summary>
	/// Participates only in publish mode, for example Azure Application Insights or an Azure Container Apps
	/// environment that only exists once deployed.
	/// </summary>
	Publish = 3,
}

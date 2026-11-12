using Aspire.Hosting.ApplicationModel;

namespace Purview.Aspire.ResourceKit;

/// <summary>
/// The dashboard node that represents a <see cref="ResourceKitGroup{THostKit}"/> in run mode.
/// </summary>
/// <remarks>
/// The resource has no lifecycle of its own: it is added in run mode only, is excluded from the publishing
/// manifest, and exists so the group's resources can be parented to it in the Aspire dashboard.
/// </remarks>
/// <param name="name">The resource name, which is the group's <see cref="ResourceKitGroup{THostKit}.Name"/>.</param>
public sealed class ResourceKitGroupResource(string name) : Resource(name)
{
	/// <summary>
	/// The resource type shown for a group in the dashboard.
	/// </summary>
	public const string ResourceTypeName = "Group";

	/// <summary>
	/// The Fluent UI icon shown for a group in the dashboard.
	/// </summary>
	public const string IconName = "Folder";
}

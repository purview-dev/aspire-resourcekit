namespace Purview.Aspire.ResourceKit;

/// <summary>
/// The lifecycle position of a resource kit. The host kit owns the graph, so a kit only moves forward
/// through these states and never builds or configures twice.
/// </summary>
enum ResourceKitLifecycleState
{
	Unbuilt,
	Disabled,
	Building,
	Built,
	Configuring,
	Configured,
	Failed,
}

/// <summary>
/// The host kit's view of a resource kit's composition gates. <see cref="ResourceKitBase{THostKit, TResource}"/>
/// implements it so the host can resolve enablement without mutating the configured <c>IsEnabled</c> value or
/// invoking user hooks for a kit an ancestor has already excluded.
/// </summary>
interface IResourceKitGating
{
	ResourceKitLifecycleState State { get; }

	bool EffectiveIsEnabled { get; }

	void ApplyGates(bool hostEnabled, bool groupsEnabled, bool ancestorsEnabled);
}

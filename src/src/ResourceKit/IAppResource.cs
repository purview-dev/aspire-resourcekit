using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting;

namespace Purview.Aspire.ResourceKit;

public interface IAppResource<in THostApp>
	where THostApp : class
{
	string Name { get; }

	bool IsEnabled { get; set; }

	void Build([NotNull] IDistributedApplicationBuilder builder);

	void Configure([NotNull] THostApp app);
}

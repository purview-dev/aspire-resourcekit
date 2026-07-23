using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting;

namespace Purview.Aspire.ResourceKit;

public interface IHostApp
{
	void Build([NotNull] IDistributedApplicationBuilder builder);

	void Configure();
}

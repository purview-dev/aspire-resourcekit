using Aspire.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Purview.Aspire.ResourceKit;

/// <summary>
/// Creates <see cref="IDistributedApplicationBuilder"/> test doubles that expose a controlled execution
/// context and hosting environment so <see cref="ResourceKitMode"/> gating can be exercised.
/// </summary>
static class TestApplicationBuilderFactory
{
	/// <summary>
	/// Creates a builder double for the specified operation and environment name.
	/// </summary>
	public static IDistributedApplicationBuilder Create(
		DistributedApplicationOperation operation,
		string environmentName = "Development"
	)
	{
		var builder = IDistributedApplicationBuilder.Mock();
		builder.ExecutionContext.Returns(new DistributedApplicationExecutionContext(operation));
		builder.Environment.Returns(new TestHostEnvironment(environmentName));

		return builder;
	}

	sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
	{
		public string EnvironmentName { get; set; } = environmentName;

		public string ApplicationName { get; set; } = "Purview.Aspire.ResourceKit.Tests";

		public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

		public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
	}
}

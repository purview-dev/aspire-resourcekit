using System.ComponentModel.DataAnnotations;

namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels;

partial class ExampleAPIAppResourceOptions
{
	[Required(AllowEmptyStrings = false)]
	public string PublishEnvironmentVariableName { get; set; } = "PUBLISH_MARKER";
}

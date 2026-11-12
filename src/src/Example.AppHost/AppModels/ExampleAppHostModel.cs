namespace Purview.Aspire.ResourceKit.Example.AppHost.AppModels;

/// <summary>
/// Gives the integration tests a supported way to compose the example's model without starting the AppHost.
/// The generated host and its extension method are internal to this example.
/// </summary>
public static class ExampleAppHostModel
{
	public static void AddResourceKit(IDistributedApplicationBuilder builder) => builder.AddAspireResourceKit();
}

namespace Purview.Aspire.ResourceKit.Pipeline.Helpers;

static class TestHelpers
{
	public static string BuildFilter(
		string? assembly = null,
		string? @namespace = null,
		string? className = null,
		string? testName = null
	)
	{
		var filter = "/";
		filter += assembly switch
		{
			null => "*",
			_ => assembly,
		};

		filter += @namespace switch
		{
			null => "*",
			_ => @namespace,
		};

		filter += className switch
		{
			null => "*",
			_ => className,
		};

		filter += testName switch
		{
			null => "*",
			_ => testName,
		};

		return filter;
	}
}

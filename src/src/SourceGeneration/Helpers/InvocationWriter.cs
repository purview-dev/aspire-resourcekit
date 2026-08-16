namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

// Temporary local fallback: the installed CodeWriter has no call-site argument-list API.
static class InvocationWriter
{
	const int WrapColumn = 100;

	public static CodeWriter WriteInvocation(
		this CodeWriter writer,
		string target,
		IReadOnlyList<string> arguments,
		bool terminate = true
	)
	{
		writer.Write(target);
		return writer.WriteArgumentList(arguments, terminate);
	}

	public static CodeWriter WriteArgumentList(
		this CodeWriter writer,
		IReadOnlyList<string> arguments,
		bool terminate = true
	)
	{
		var inline = $"({string.Join(", ", arguments)})";
		if (inline.Length <= WrapColumn)
		{
			writer.Write(inline);
		}
		else
		{
			writer.WriteLine("(");
			writer.Indented(indented =>
			{
				for (var index = 0; index < arguments.Count; index++)
				{
					indented.Write(arguments[index]);
					if (index < arguments.Count - 1)
						indented.Write(",");
					indented.NewLine();
				}
			});
			writer.Write(")");
		}

		if (terminate)
			writer.Write(";");

		return writer;
	}

	public static CodeWriter WriteInvocationLine(
		this CodeWriter writer,
		string target,
		IReadOnlyList<string> arguments
	) => writer.WriteInvocation(target, arguments).NewLine();
}

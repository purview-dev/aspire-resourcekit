using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Purview.Aspire.ResourceIsolation.SourceGeneration.Models;

sealed record DiagnosticInfo(
	DiagnosticDescriptor Descriptor,
	string FilePath,
	TextSpan TextSpan,
	LinePositionSpan LinePositionSpan,
	EquatableArray<string> MessageArgs
)
{
	public Diagnostic ToDiagnostic()
	{
		var location = Location.Create(FilePath, TextSpan, LinePositionSpan);

		// ImmutableArray<string> cannot be cast to object?[] directly;
		// materialise through a temporary array to satisfy the Diagnostic.Create overload.
		var args = MessageArgs.AsImmutableArray();
		var objArgs = new object?[args.Length];
		for (var i = 0; i < args.Length; i++)
			objArgs[i] = args[i];

		return Diagnostic.Create(Descriptor, location, objArgs);
	}

	public static DiagnosticInfo Create(
		DiagnosticDescriptor descriptor,
		Location? location,
		params string[] messageArgs
	)
	{
		if (location is null)
		{
			return new DiagnosticInfo(
				Descriptor: descriptor,
				FilePath: string.Empty,
				TextSpan: default,
				LinePositionSpan: default,
				MessageArgs: EquatableArray<string>.Create(messageArgs)
			);
		}

		var lineSpan = location.GetLineSpan();
		return new DiagnosticInfo(
			Descriptor: descriptor,
			FilePath: lineSpan.Path,
			TextSpan: location.SourceSpan,
			LinePositionSpan: lineSpan.Span,
			MessageArgs: EquatableArray<string>.Create(messageArgs)
		);
	}
}

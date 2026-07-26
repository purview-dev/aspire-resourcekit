using System.Collections.Immutable;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Models;

readonly record struct GeneratorResult<T>
{
	public T? Value { get; private init; }

	public ImmutableArray<DiagnosticInfo> Diagnostics { get; private init; }

	public bool IsSuccess => Value is not null;

	public bool HasDiagnostics => !Diagnostics.IsDefaultOrEmpty;

	public bool IsFatal => Value is null && HasDiagnostics;

	public bool IsEmpty => Value is null && !HasDiagnostics;

	public static GeneratorResult<T> Ok(T value, params DiagnosticInfo[] diagnostics) =>
		new() { Value = value, Diagnostics = diagnostics?.ToImmutableArray() ?? [] };

	public static GeneratorResult<T> Fail(params DiagnosticInfo[] diagnostics)
	{
		return diagnostics is null || diagnostics.Length == 0
			? throw new ArgumentException(
				"At least one diagnostic must be provided for a failure result.",
				nameof(diagnostics)
			)
			: new() { Diagnostics = [.. diagnostics] };
	}

	public static GeneratorResult<T> Empty;
}

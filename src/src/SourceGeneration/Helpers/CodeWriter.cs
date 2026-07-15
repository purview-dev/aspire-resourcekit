using System.Collections.Immutable;
using System.Text;

namespace Purview.Aspire.ResourceIsolation.SourceGeneration.Helpers;

sealed class CodeWriter
{
	static readonly ImmutableDictionary<int, string> IndentCache = CreateIndentCache();

	readonly StringBuilder _builder = new();
	int _indentLevel;

	public CodeWriter Indent()
	{
		_indentLevel++;

		return this;
	}

	public CodeWriter Unindent()
	{
		if (_indentLevel == 0)
		{
#if DEBUG
			return this;
#else

			throw new InvalidOperationException("Cannot unindent below zero.");
#endif
		}

		_indentLevel--;

		return this;
	}

	public CodeWriter NewLine()
	{
		_builder.AppendLine();

		return this;
	}

	public CodeWriter WriteLine(string? value = null)
	{
		WriteIndent();
		_builder.AppendLine(value);

		return this;
	}

	public CodeWriter WriteIndent()
	{
		_builder.Append(IndentCache[_indentLevel]);
		return this;
	}

	public CodeWriter Write(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			throw new ArgumentNullException(nameof(value));

		_builder.Append(value);

		return this;
	}

	public CodeWriter Quote(string? value = null)
	{
		Write("\"");
		if (!string.IsNullOrEmpty(value))
			Write(value!);

		return Write("\"");
	}

	public CodeWriter QuoteLine(string? value = null) => Quote(value).WriteLine();

	public IDisposable Block(string? header = null, string seperator = "{", string? closingSeperator = null)
	{
		if (header != null)
			WriteLine(header);

		WriteLine(seperator);
		Indent();

		if (closingSeperator == null)
		{
			// When the closing seperator is null, it's effectively 'auto'.
			if (seperator == "{")
				closingSeperator = "}";
			else if (seperator == "(")
				closingSeperator = ")";
		}

		return new BlockScope(this, closingSeperator);
	}

	void Reset()
	{
		_builder.Clear();
		_indentLevel = 0;
	}

	public IDisposable Begin() => new ClearScope(this);

	public override string ToString() => _builder.ToString();

	static ImmutableDictionary<int, string> CreateIndentCache() =>
		Enumerable.Range(0, 7).Select(i => new KeyValuePair<int, string>(i, new('\t', i))).ToImmutableDictionary();

	sealed class ClearScope(CodeWriter writer) : IDisposable
	{
		bool _disposed;

		public void Dispose()
		{
			if (_disposed)
				return;

			writer.Reset();

			_disposed = true;
		}
	}

	sealed class BlockScope(CodeWriter writer, string? closingSeperator) : IDisposable
	{
		bool _disposed;

		public void Dispose()
		{
			if (_disposed)
				return;

			writer.Unindent();
			writer.WriteLine(closingSeperator);

			_disposed = true;
		}
	}
}

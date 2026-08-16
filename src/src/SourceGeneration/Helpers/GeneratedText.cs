using System.Text;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

// Temporary local fallback: CodeWriter currently exposes no escaping helpers.
static class GeneratedText
{
	public static string QuoteLiteral(string value)
	{
		var builder = new StringBuilder(value.Length + 2);
		builder.Append('"');
		foreach (var character in value)
		{
			switch (character)
			{
				case '\\':
					builder.Append("\\\\");
					break;
				case '"':
					builder.Append("\\\"");
					break;
				case '\r':
					builder.Append("\\r");
					break;
				case '\n':
					builder.Append("\\n");
					break;
				case '\t':
					builder.Append("\\t");
					break;
				default:
					builder.Append(character);
					break;
			}
		}

		return builder.Append('"').ToString();
	}

	public static string Xml(string value) =>
		value
			.Replace("&", "&amp;")
			.Replace("<", "&lt;")
			.Replace(">", "&gt;")
			.Replace("\"", "&quot;")
			.Replace("'", "&apos;");
}

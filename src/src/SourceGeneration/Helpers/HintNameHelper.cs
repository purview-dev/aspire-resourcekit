using System.Security.Cryptography;
using System.Text;

namespace Purview.Aspire.ResourceKit.SourceGeneration.Helpers;

// Temporary local fallback: the installed framework has no collision-safe hint-name helper.
public static class HintNameHelper
{
	public static string ForHost(string metadataFullName)
	{
		if (metadataFullName is null)
			throw new ArgumentNullException(nameof(metadataFullName));
		var identity = metadataFullName;
		var safeIdentity = new StringBuilder(identity.Length);
		foreach (var character in identity)
		{
			if (char.IsLetterOrDigit(character) || character is '.' or '_' or '-')
				safeIdentity.Append(character);
			else
				safeIdentity.Append('_');
		}

		byte[] digest;
		using (var sha256 = SHA256.Create())
			digest = sha256.ComputeHash(Encoding.UTF8.GetBytes(identity));

		var hash = new StringBuilder(12);
		for (var index = 0; index < 6; index++)
			hash.Append(digest[index].ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
		return $"{safeIdentity}.AspireResourceKit.{hash}.g.cs";
	}
}

namespace Purview.Aspire.ResourceIsolation;

public interface IIsolationSettingsProvider
{
	IsolationSettings Load();
}

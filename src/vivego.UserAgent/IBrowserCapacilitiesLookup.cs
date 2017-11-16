namespace vivego.UserAgent
{
	public interface IBrowserCapacilitiesLookup
	{
		BrowserCapabilities Lookup(string userAgent);
	}
}
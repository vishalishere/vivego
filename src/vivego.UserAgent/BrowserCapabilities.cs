using System.Collections.Generic;
using System.Linq;

namespace vivego.UserAgent
{
	public class BrowserCapabilities
	{
		public string Regex => "^" +
			System.Text.RegularExpressions.Regex.Escape(Pattern.ToLowerInvariant())
				.Replace(@"\*", ".*")
				.Replace(@"\?", ".?")
			+ "$";

		public string Pattern { get; set; }
		public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

		public string Parent => GetProperty("Parent");
		public string Browser => GetProperty("Browser");
		public string Comment => GetProperty("Comment");
		public string BrowserMaker => GetProperty("Browser_Maker");
		public string Version => GetProperty("Version");
		public int MajorVersion => GetIntProperty("MajorVer");
		public int MinorVersion => GetIntProperty("MinorVer");
		public string Platform => GetProperty("Platform");
		public bool IsMobileDevice => GetBoolProperty("isMobileDevice");
		public bool IsTablet => GetBoolProperty("isTablet");
		public bool Crawler => GetBoolProperty("Crawler");
		public string DeviceType => GetProperty("Device_Type");
		public string DevicePointingMethod => GetProperty("Device_Pointing_Method");

		private string GetProperty(string propertyName, string defaultValue = null)
		{
			return Properties.TryGetValue(propertyName, out string t) ? t : defaultValue;
		}

		private bool GetBoolProperty(string propertyName, bool defaultValue = false)
		{
			if (Properties.TryGetValue(propertyName, out string value)
				&& bool.TryParse(value, out bool b))
			{
				return b;
			}

			return defaultValue;
		}

		private int GetIntProperty(string propertyName, int defaultValue = 0)
		{
			if (Properties.TryGetValue(propertyName, out string value)
				&& int.TryParse(value, out int i))
			{
				return i;
			}

			return defaultValue;
		}

		public void Merge(BrowserCapabilities otherBrowserCapabilities)
		{
			foreach (KeyValuePair<string, string> pair in otherBrowserCapabilities.Properties)
			{
				if (!Properties.ContainsKey(pair.Key))
				{
					Properties.Add(pair);
				}
			}
		}

		public override string ToString()
		{
			return string.Join(";", Properties.Select(pair => $"{pair.Key}: {pair.Value}"));
		}
	}
}
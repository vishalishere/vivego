using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace vivego.UserAgent
{
	public static class SearchExtensions
	{
		public static BrowserCapabilities Find(this IList<BrowserCapabilities> db, string userAgent)
		{
			return db.FirstOrDefault(browserCapabilitiese => Regex.IsMatch(userAgent, browserCapabilitiese.Regex));
		}
	}
}
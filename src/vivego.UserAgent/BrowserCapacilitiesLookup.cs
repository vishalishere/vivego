using System;
using System.Collections.Generic;

namespace vivego.UserAgent
{
	public class BrowserCapacilitiesLookup : IBrowserCapacilitiesLookup
	{
		private readonly IList<BrowserCapabilities> _db;

		public BrowserCapacilitiesLookup(IList<BrowserCapabilities> db)
		{
			_db = db ?? throw new ArgumentNullException(nameof(db));
		}

		public BrowserCapabilities Lookup(string userAgent)
		{
			if (userAgent == null)
			{
				throw new ArgumentNullException(nameof(userAgent));
			}

			return _db.Find(userAgent.ToLowerInvariant());
		}
	}
}
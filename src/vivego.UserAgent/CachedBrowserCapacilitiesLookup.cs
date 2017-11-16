using System;

using Microsoft.Extensions.Caching.Memory;

namespace vivego.UserAgent
{
	public class CachedBrowserCapacilitiesLookup : IBrowserCapacilitiesLookup
	{
		private readonly IMemoryCache _cache;
		private readonly IBrowserCapacilitiesLookup _browserCapacilitiesLookup;
		private TimeSpan? _cacheTimeout = null;

		public CachedBrowserCapacilitiesLookup(IMemoryCache cache,
			IBrowserCapacilitiesLookup browserCapacilitiesLookup,
			TimeSpan? cacheTimeout = null)
		{
			_cacheTimeout = cacheTimeout;
			_cache = cache ?? throw new ArgumentNullException(nameof(cache));
			_browserCapacilitiesLookup = browserCapacilitiesLookup ?? throw new ArgumentNullException(nameof(browserCapacilitiesLookup));
		}

		public BrowserCapabilities Lookup(string userAgent)
		{
			if (!_cache.TryGetValue(userAgent, out BrowserCapabilities browserCapabilities))
			{
				browserCapabilities = _browserCapacilitiesLookup.Lookup(userAgent);
				if (_cacheTimeout.HasValue)
				{
					_cache.Set(userAgent, browserCapabilities, _cacheTimeout.Value);
				}
				else
				{
					_cache.Set(userAgent, browserCapabilities);
				}
			}

			return browserCapabilities;
		}
	}
}
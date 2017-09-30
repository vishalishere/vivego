using System.Collections.Generic;
using System.Linq;

namespace vivego.FullTextSearch
{
	/// <summary>
	/// Non thread safe in memory data store for text data
	/// </summary>
	public class InMemoryDataStore : IDataStore
	{
		private readonly Dictionary<string, List<object>> _db = new Dictionary<string, List<object>>();

		public void Add<T>(string key, T t)
		{
			if (!_db.TryGetValue(key, out List<object> list))
			{
				list = new List<object>();
				_db.Add(key, list);
			}

			list.Add(t);
		}

		public void Remove<T>(string key)
		{
			_db.Remove(key);
		}

		public IEnumerable<T> Get<T>(string key)
		{
			if (_db.TryGetValue(key, out List<object> list))
			{
				return list.OfType<T>();
			}

			return Enumerable.Empty<T>();
		}
	}
}
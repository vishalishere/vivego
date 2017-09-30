using System.Collections.Generic;

namespace vivego.FullTextSearch
{
	public interface IDataStore
	{
		void Add<T>(string key, T t);
		void Remove<T>(string key);
		IEnumerable<T> Get<T>(string key);
	}
}
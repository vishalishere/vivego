using System.Collections.Generic;

namespace vivego.FullTextSearch
{
	public interface IFullTextSearch<T>
	{
		void Ingest(string[] searchTerms, T t);
		IEnumerable<(string Term, T Data)> Search(string[] searchTerms, int maxHits = 100);
		void Commit();
	}
}
using Lucene.Net.Store;

using vivego.Serializer.Abstractions;

namespace vivego.FullTextSearch
{
	public class InMemoryLuceneFullTextSearch<T> : LuceneFullTextSearch<T>
	{
		public InMemoryLuceneFullTextSearch(ISerializer<string> serializer) : base(serializer, new RAMDirectory())
		{
		}
	}
}
using Lucene.Net.Store;

using vivego.Serializer.Abstractions;

namespace vivego.FullTextSearch
{
	public class FsDirectoryLuceneFullTextSearch<T> : LuceneFullTextSearch<T>
	{
		public FsDirectoryLuceneFullTextSearch(string indexPath,
			ISerializer<string> serializer) : base(serializer, FSDirectory.Open(indexPath))
		{
		}
	}
}
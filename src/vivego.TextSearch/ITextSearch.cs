using System.Collections.Generic;

namespace vivego.FullTextSearch
{
	public interface IFullTextSearch<T>
	{
		void Ingest(string text, T t);
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		IEnumerable<T> Search(string text);
	}
}
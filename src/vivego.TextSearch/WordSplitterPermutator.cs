using System;
using System.Collections.Generic;

namespace vivego.FullTextSearch
{
	public class WordSplitterPermutator : IPermutator
	{
		public IEnumerable<string> Permutate(string source)
		{
			return source.Split(new[] {' ', '\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);
		}
	}
}
using System.Collections.Generic;

namespace vivego.FullTextSearch
{
	public class DummyPermutator : IPermutator
	{
		public IEnumerable<string> Permutate(string source)
		{
			yield return source;
		}
	}
}
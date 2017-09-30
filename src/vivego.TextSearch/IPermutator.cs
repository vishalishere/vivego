using System.Collections.Generic;

namespace vivego.FullTextSearch
{
	public interface IPermutator
	{
		IEnumerable<string> Permutate(string source);
	}
}
using System.Collections.Generic;

namespace vivego.FullTextSearch
{
	public class StartsWithPermutator : IPermutator
	{
		public IEnumerable<string> Permutate(string source)
		{
			for (int index = 1; index <= source.Length; index++)
			{
				yield return source.Substring(0, index);
			}
		}
	}
}
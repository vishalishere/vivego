using System.Collections.Generic;

namespace vivego.FullTextSearch
{
	public class ContainsPermutator : IPermutator
	{
		/// <summary>
		///     hej -> h, e, j, he, ej, hej
		///     hello -> h, e, l, o, he, el, ll, lo, hel, ell, llo, hell, hello
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public IEnumerable<string> Permutate(string source)
		{
			for (int permLength = 1; permLength <= source.Length; permLength++)
			{
				for (int index = 0; index <= source.Length - permLength; index++)
				{
					string substring = source.Substring(index, permLength);
					yield return substring;
				}
			}
		}
	}
}
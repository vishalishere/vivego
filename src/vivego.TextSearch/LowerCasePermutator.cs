using System.Collections.Generic;
using System.Globalization;

namespace vivego.FullTextSearch
{
	public class LowerCasePermutator : IPermutator
	{
		private readonly CultureInfo _cultureInfo;

		public LowerCasePermutator()
		{
			_cultureInfo = CultureInfo.InvariantCulture;
		}

		public LowerCasePermutator(CultureInfo cultureInfo)
		{
			_cultureInfo = cultureInfo;
		}

		public IEnumerable<string> Permutate(string source)
		{
			yield return source.ToLower(_cultureInfo);
		}
	}
}
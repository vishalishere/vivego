using System.Collections.Generic;
using System.Globalization;

namespace vivego.FullTextSearch
{
	public class UpperCasePermutator : IPermutator
	{
		private readonly CultureInfo _cultureInfo;

		public UpperCasePermutator()
		{
			_cultureInfo = CultureInfo.InvariantCulture;
		}

		public UpperCasePermutator(CultureInfo cultureInfo)
		{
			_cultureInfo = cultureInfo;
		}

		public IEnumerable<string> Permutate(string source)
		{
			yield return source.ToUpper(_cultureInfo);
		}
	}
}
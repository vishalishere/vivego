using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace vivego.core
{
	/// <summary>
	/// You can specify multiple URLs or parts of URLs by writing part sets within braces as in: 
	/// Resolve arrays: http://site.[one,two,three].com
	/// or you can get sequences of alphanumeric series by using [] as in:
	/// ftp://ftp.numericals.com/file[1-100].txt  ftp://ftp.numericals.com/file[001-100].txt (with leading zeros)  ftp://ftp.letters.com/file[a-z].txt
	/// No nesting of the sequences is supported at the moment, but you can use several ones next to each other:
	/// http://any.org/archive[1996-1999]/vol[1-4]/part[a,b,c].html
	/// http://any.org/archive[1996-1999]/vol[1-4,7,9]/part[a,b,c].html
	/// Hex is also supported: http://any.org/archive[0x1-0xa]/vol[1-4,7,9]/part[a,b,c].html
	/// You can get every Nth number or letter:
	/// http://www.numericals.com/file[1-100:10].txt  http://www.letters.com/file[a-z:2].txt 
	/// </summary>
	public class StringPatternGenerator : IEnumerable<string>
	{
		private readonly string _source;

		private static readonly Lazy<Regex> s_alphaSequenceRegex = new Lazy<Regex>(
			() => new Regex("(?<Begin>\\w+)-(?<End>\\w+)(:(?<Step>\\d*))?",
				RegexOptions.IgnoreCase | RegexOptions.CultureInvariant |
				RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled), true);

		private static readonly Lazy<Regex> s_sequencesRegex = new Lazy<Regex>(
			() => new Regex("\\[(?<sequences>.*?)\\]",
				RegexOptions.IgnoreCase | RegexOptions.CultureInvariant |
				RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled), true);

		public StringPatternGenerator(string source)
		{
			_source = source;
		}

		public IEnumerator<string> GetEnumerator()
		{
			IEnumerable<SequenceItem> tmp = s_sequencesRegex.Value.
				Matches(_source).
				Cast<Match>().
				OrderBy(s => s.Index).
				Select(match => new SequenceItem(match));

			return FlattenAndReplace(_source, tmp).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		private static IEnumerable<string> FlattenAndReplace(string source,
			IEnumerable<SequenceItem> sequences)
		{
			IEnumerable<SequenceItem> enumerable = sequences as SequenceItem[] ?? sequences.ToArray();
			SequenceItem firstSequence = enumerable.FirstOrDefault();
			if (firstSequence == null)
			{
				return new[] { source };
			}

			return FlattenAndReplace(source, enumerable.Skip(1)).
				SelectMany(child => (
					from replacement in firstSequence
					let result = child.Remove(firstSequence.ReplacementMatch.Index, firstSequence.ReplacementMatch.Length)
					select result.Insert(firstSequence.ReplacementMatch.Index, replacement)));
		}

		private static IEnumerable<string> AlphaSequenceGenerator(char alphaNumericBegin, char alphaNumericEnd, int step)
		{
			for (int i = alphaNumericBegin; i <= alphaNumericEnd; i += step)
			{
				yield return ((char)i).ToString(CultureInfo.InvariantCulture);
			}
		}

		private static IEnumerable<string> HexSequenceGenerator(string pattern, int start, int end, int step)
		{
			for (int i = start; i <= end; i += step)
			{
				yield return i.ToString("X", CultureInfo.InvariantCulture).PadLeft(pattern.Length, '0');
			}
		}

		private static IEnumerable<string> IntegerSequenceGenerator(string pattern, int start, int end, int step)
		{
			for (int i = start; i <= end; i += step)
			{
				yield return i.ToString(pattern, CultureInfo.InvariantCulture);
			}
		}

		protected class RangeSequenceItem : IEnumerable<string>
		{
			private readonly Match _rangeMatch;

			public RangeSequenceItem(Match rangeMatch)
			{
				_rangeMatch = rangeMatch;
			}

			public IEnumerator<string> GetEnumerator()
			{
				string alphaNumericBegin = _rangeMatch.Groups["Begin"].Value;
				string alphaNumericEnd = _rangeMatch.Groups["End"].Value;
				string stepValue = _rangeMatch.Groups["Step"].Value;
				if (!int.TryParse(stepValue, out int step))
				{
					step = 1;
				}

				// Test integer range
				if (int.TryParse(alphaNumericBegin, out int begin) &&
					int.TryParse(alphaNumericEnd, out int end))
				{
					string pattern = new string('0', alphaNumericBegin.Length);
					return IntegerSequenceGenerator(pattern, begin, end, step).GetEnumerator();
				}

				// Test hex range
				if (alphaNumericBegin.StartsWith("0x") && alphaNumericEnd.StartsWith("0x") &&
					int.TryParse(alphaNumericBegin.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out begin) &&
					int.TryParse(alphaNumericEnd.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out end))
				{
					string pattern = new string('0', alphaNumericBegin.Length - 2/*0x*/);
					return HexSequenceGenerator(pattern, begin, end, step).GetEnumerator();
				}

				// Test ascii range
				if (!alphaNumericBegin.IsNullOrEmpty() && !alphaNumericEnd.IsNullOrEmpty())
				{
					return AlphaSequenceGenerator(alphaNumericBegin[0], alphaNumericEnd[0], step).GetEnumerator();
				}

				return new[]
					{
						alphaNumericBegin,
						alphaNumericEnd
					}.Cast<string>().GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		protected class SequenceItem : IEnumerable<string>
		{
			private readonly List<IEnumerable<string>> _items = new List<IEnumerable<string>>();

			public SequenceItem(Match replacementMatch)
			{
				ReplacementMatch = replacementMatch;
				string[] sequences = replacementMatch.Groups["sequences"].Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string sequenceItem in sequences)
				{
					// Determine sequence _items type
					Match alphaSequenceMatch = s_alphaSequenceRegex.Value.Match(sequenceItem);
					if (alphaSequenceMatch.Success)
					{
						_items.Add(new RangeSequenceItem(alphaSequenceMatch));
					}
					else
					{
						_items.Add(new[] { sequenceItem });
					}
				}
			}

			public Match ReplacementMatch { get; set; }

			public IEnumerator<string> GetEnumerator()
			{
				return _items.SelectMany(i => i.ToArray()).GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}
	}
}
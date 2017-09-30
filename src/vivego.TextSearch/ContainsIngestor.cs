using System;
using System.Collections.Generic;
using System.Linq;

namespace vivego.FullTextSearch
{
	public class TextSearch<T> : IFullTextSearch<T>
	{
		private readonly IDataStore _dataStore;
		private readonly IPermutator _permutator;
		private readonly Func<T, IEnumerable<string>> _textExtractor;

		public TextSearch(IDataStore dataStore,
			IPermutator permutator,
			Func<T, IEnumerable<string>> textExtractor)
		{
			_dataStore = dataStore;
			_permutator = permutator;
			_textExtractor = textExtractor;
		}

		public void Ingest(string id, T t)
		{
			_dataStore.Add(id, t);
			IEnumerable<string> textExtractor = _textExtractor(t);
			foreach (string text in textExtractor)
			foreach (string s in _permutator.Permutate(text))
			{
				_dataStore.Add(s, id);
			}
		}

		public IEnumerable<T> Search(string text)
		{
			IEnumerable<string> keys = _dataStore.Get<string>(text);
			return keys
				.SelectMany(key => _dataStore.Get<T>(key));
		}
	}
}
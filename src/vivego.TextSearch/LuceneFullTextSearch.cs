using System;
using System.Collections.Generic;

using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;

using vivego.core;
using vivego.Serializer.Abstractions;

namespace vivego.FullTextSearch
{
	public abstract class LuceneFullTextSearch<T> : DisposableBase, IFullTextSearch<T>
	{
		private const LuceneVersion LuceneVersion = Lucene.Net.Util.LuceneVersion.LUCENE_48;
		private const string TermsFieldName = "terms";
		private const string DataFieldName = "data";

		private DirectoryReader _reader;
		private IndexSearcher _searcher;
		private readonly ISerializer<string> _serializer;
		private readonly IndexWriter _writer;

		protected LuceneFullTextSearch(ISerializer<string> serializer,
			Directory directory)
		{
			_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

			RegisterDisposable(directory);

			_writer = new IndexWriter(directory, new IndexWriterConfig(LuceneVersion, new SimpleAnalyzer(LuceneVersion)));
			RegisterDisposable(_writer);
			_writer.Commit();

			_reader = DirectoryReader.Open(directory);
			RegisterDisposable(() => _reader.Dispose());

			_searcher = new IndexSearcher(_reader);
		}

		public void Commit()
		{
			_writer.Commit();
		}

		public void Ingest(string[] searchTerms, T t)
		{
			Document doc = new Document
			{
				new StringField(DataFieldName, _serializer.Serialize(t), Field.Store.YES)
			};
			foreach (string searchTerm in searchTerms.EmptyIfNull())
			{
				doc.Add(new StringField(TermsFieldName, searchTerm, Field.Store.YES));
			}

			_writer.AddDocument(doc);
		}

		public IEnumerable<(string Term, T Data)> Search(string[] searchTerms, int maxHits = 100)
		{
			if (!_reader.IsCurrent())
			{
				using (_reader)
				{
					_reader = DirectoryReader.OpenIfChanged(_reader);
					_searcher = new IndexSearcher(_reader);
				}
			}

			MultiPhraseQuery query = new MultiPhraseQuery();
			foreach (string searchTerm in searchTerms)
			{
				query.Add(new Term(TermsFieldName, searchTerm));
			}

			TopDocs hits = _searcher.Search(query, maxHits);
			foreach (ScoreDoc topDoc in hits.ScoreDocs)
			{
				Document document = _reader.Document(topDoc.Doc);
				string serializedData = document.GetField(DataFieldName).GetStringValue();
				T t = _serializer.Deserialize<T>(serializedData);
				string term = document.GetField(TermsFieldName).GetStringValue();
				yield return (term, t);
			}
		}
	}
}
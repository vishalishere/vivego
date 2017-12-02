using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;

using Microsoft.Extensions.Caching.Memory;

using vivego.FullTextSearch;
using vivego.Serializer.NewtonSoft;

namespace vivego.UserAgent
{
	public class EntryPoint
	{
		public static void Main(string[] args)
		{
			var db  = JsonBrowserCapabilitiesReader.ReadJson(new FileStream(@"C:\Users\esben\Downloads\browscap.json", FileMode.Open,
				FileAccess.Read));

			InMemoryLuceneFullTextSearch<BrowserCapabilities> fullTextSearch = new InMemoryLuceneFullTextSearch<BrowserCapabilities>(new NewtonSoftJsonSerializer());
			foreach (BrowserCapabilities browserCapabilitiese in db.Take(1000))
			{
				fullTextSearch.Ingest(Tokenize(browserCapabilitiese.Pattern).ToArray(), browserCapabilitiese);
			}

			fullTextSearch.Commit();

			//IBrowserCapacilitiesLookup lookup = new BrowserCapacilitiesLookup(db);
			//lookup = new CachedBrowserCapacilitiesLookup(new MemoryCache(new MemoryCacheOptions()), lookup);

			////var bc = lookup.Lookup("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/62.0.3202.94 Safari/537.36");
			////Console.Out.WriteLine(bc.Pattern);

			////var analyzer = new SimpleAnalyzer(LuceneVersion.LUCENE_48);

			////Mozilla/5.0 (*Windows NT 10.0*Win64? x64*) applewebkit* (*khtml*like*gecko*) Chrome/62.0*Safari/*
			var value = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/62.0.3202.94 Safari/537.36";
			//Console.Out.WriteLine("---");
			//Tokanize("Mozilla/5.0 (*Windows NT 10.0*Win64? x64*) applewebkit* (*khtml*like*gecko*) Chrome/62.0*Safari/*");

			foreach ((string Term, BrowserCapabilities Data) valueTuple in fullTextSearch.Search(Tokenize(value).ToArray()))
			{
				Console.Out.WriteLine(valueTuple.Term);
			}
		}

		private static IEnumerable<string> Tokenize(string value)
		{
			using (StringReader stringReader = new StringReader(value))
			using (LetterTokenizer letterTokenizer = new LetterTokenizer(LuceneVersion.LUCENE_48, stringReader))
			{
				letterTokenizer.Reset();
				while (letterTokenizer.IncrementToken())
				{
					yield return letterTokenizer.GetAttribute<ICharTermAttribute>().ToString();
				}
			}
		}
	}
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Extensions.Caching.Memory;

using Newtonsoft.Json.Linq;

using vivego.core;
using vivego.FullTextSearch;
using vivego.Serializer.NewtonSoft;

namespace vivego.UserAgent
{
	public class UserAgentParser : DisposableBase
	{
		private readonly IFullTextSearch<string> _textSearch;
		private NewtonSoftJsonSerializer jsonSerializer;

		public UserAgentParser(string fileName)
		{
			jsonSerializer = new NewtonSoftJsonSerializer();

			_textSearch = new FsDirectoryLuceneFullTextSearch<string>("d:\\temp\\Index", jsonSerializer);
			if (_textSearch is IDisposable disposable)
			{
				RegisterDisposable(disposable);
			}

			//if (!_textSearch.Search(new []{ "*" }).Any())
			{
				//IDictionary<string, BrowserCapabilities> db = JsonBrowserCapabilitiesReader.Read(fileName);
				//_textSearch.Commit();
			}

			//foreach ((string Term, string Data) tuple in _textSearch.Search(new []{ "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/61.0.3163.100 Safari/537.36 OPR/48.0.2685.52" 
			//	.Replace(";", "")
			//	.Replace("(", "")
			//	.Replace(")", "")
			//	.Replace("/", "")
			//	.Replace(" ", "*")}))
			//{
			//	Console.Out.WriteLine(tuple);
			//}
		}

		//public IDictionary<string, string> Lookup(string userAgent)
		//{
		//	return _textSearch
		//		.Search(userAgent)
		//		.Select()
		//}

		//private IDictionary<string, string> Resolve(string key)
		//{
			
		//}

		//private IDictionary<string, string> From(string json)
		//{
		//	JObject jObject = (JObject) jsonSerializer.Deserialize(json);
		//	return jObject
		//		.ToDictionary(x => x.Key, null);

		//}
	}

	public class UserAgentLookup
	{
	}

	public class EntryPoint
	{
		public static void Main(string[] args)
		{
			//UserAgentParser ua = new UserAgentParser(@"C:\Users\esben\Downloads\browscap.json");

			var db  = JsonBrowserCapabilitiesReader.ReadJson(new FileStream(@"C:\Users\esben\Downloads\browscap.json", FileMode.Open,
				FileAccess.Read));
			IBrowserCapacilitiesLookup lookup = new BrowserCapacilitiesLookup(db);
			lookup = new CachedBrowserCapacilitiesLookup(new MemoryCache(new MemoryCacheOptions()), lookup);

			var bc = lookup.Lookup("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/62.0.3202.94 Safari/537.36");

			Console.Out.WriteLine(bc);
			Console.ReadLine();


			//var regex = "^" + 
			//	Regex.Escape("mozilla/5.0 (*windows nt 10.0*win64? x64*) applewebkit* (*khtml*like*gecko*)*chrome/*safari/*opr/48.0*")
			//		.Replace(@"\*", ".*")
			//		.Replace(@"\?", ".?")
			//	+ "$";

			//Console.Out.WriteLine(Regex.IsMatch(
			//	"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/61.0.3163.100 Safari/537.36 OPR/48.0.2685.52".ToLowerInvariant(),
			//	regex));
		}
	}
}
using System.Linq;
using System.Threading.Tasks;

using vivego.Serializer.NewtonSoft;

using Xunit;

namespace vivego.FullTextSearch.Tests
{
	public class InMemoryLuceneFullTextSearchTests
	{
		[Fact]
		public void CanSearch()
		{
			using (InMemoryLuceneFullTextSearch<string> textSearch =
				new InMemoryLuceneFullTextSearch<string>(new NewtonSoftJsonSerializer()))
			{
				textSearch.Ingest(new []{ "A" }, "B");

				Task.Delay(1000);

				var all = textSearch.Search(new []{ "A" }).ToArray();
				Assert.Single(all);
				Assert.Equal("B", all[0].Data);
			}
		}
	}
}
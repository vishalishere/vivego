using System.Collections.Generic;
using System.Linq;

using vivego.PublishSubscribe.Cache;
using vivego.PublishSubscribe.Topic;

using Xunit;

namespace vivego.PublishSubscribe.Tests.Cache
{
	public class SubscriptionWriterLookupTests
	{
		private readonly SubscriptionWriterLookup<string> _subscriptionWriterLookup;

		public SubscriptionWriterLookupTests()
		{
			_subscriptionWriterLookup = new SubscriptionWriterLookup<string>();
		}

		[Fact]
		public void CanRemoveEmpty()
		{
			IEnumerable<Subscription> removed = _subscriptionWriterLookup.Remove("A");
			Assert.Empty(removed);
		}

		[Fact]
		public void CanGetAllOnEmpty()
		{
			(string, Subscription)[] all = _subscriptionWriterLookup.GetAll();
			Assert.Empty(all);
		}

		[Fact]
		public void CanLookupOnEmpty()
		{
			(string Group, bool HashBy, string[] Writer)[] lookup = _subscriptionWriterLookup.Lookup(new Message());
			Assert.Empty(lookup);
		}

		[Fact]
		public void CanAdd()
		{
			Subscription subscription = new Subscription { Group = "GroupA" };
			bool isAdded = _subscriptionWriterLookup.Add("A", subscription, new LambdaTopicfilter(s => true));
			Assert.True(isAdded);
		}

		[Fact]
		public void CanAddSameTwiceInDifferentGroups()
		{
			_subscriptionWriterLookup.Add("A", new Subscription { Group = "GroupA" }, new LambdaTopicfilter(s => true));
			bool isAdded = _subscriptionWriterLookup.Add("A", new Subscription { Group = "GroupB" }, new LambdaTopicfilter(s => true));
			Assert.True(isAdded);
		}

		[Fact]
		public void CannotAddSameTwiceInSameGroup()
		{
			_subscriptionWriterLookup.Add("A", new Subscription { Group = "GroupA" }, new LambdaTopicfilter(s => true));
			bool isAdded = _subscriptionWriterLookup.Add("A", new Subscription { Group = "GroupA" }, new LambdaTopicfilter(s => true));
			Assert.False(isAdded);
		}

		[Fact]
		public void CannotAddSameTwiceInEmptyGroup()
		{
			_subscriptionWriterLookup.Add("A", new Subscription(), new LambdaTopicfilter(s => true));
			bool isAdded = _subscriptionWriterLookup.Add("A", new Subscription(), new LambdaTopicfilter(s => true));
			Assert.False(isAdded);
		}

		[Fact]
		public void CanRemove()
		{
			Subscription subscription = new Subscription { Group = "GroupA" };
			_subscriptionWriterLookup.Add("A", subscription, new LambdaTopicfilter(s => true));
			IEnumerable<Subscription> removed = _subscriptionWriterLookup.Remove("A");
			Assert.Single(removed);
		}

		[Fact]
		public void CanRemoveManyDifferentGroups()
		{
			_subscriptionWriterLookup.Add("A", new Subscription { Group = "A" }, new LambdaTopicfilter(s => true));
			_subscriptionWriterLookup.Add("A", new Subscription { Group = "B" }, new LambdaTopicfilter(s => true));
			IEnumerable<Subscription> removed = _subscriptionWriterLookup.Remove("A");
			Assert.Equal(2, removed.Count());
		}

		[Fact]
		public void WillOnlyRemoveSelected()
		{
			Subscription subscription = new Subscription();
			_subscriptionWriterLookup.Add("A", subscription, new LambdaTopicfilter(s => true));
			_subscriptionWriterLookup.Add("B", subscription, new LambdaTopicfilter(s => true));
			_subscriptionWriterLookup.Add("C", subscription, new LambdaTopicfilter(s => true));
			IEnumerable<Subscription> removed = _subscriptionWriterLookup.Remove("B");
			Assert.Single(removed);
		}

		[Fact]
		public void WillOnlyRemoveSelected2()
		{
			Subscription subscription = new Subscription();
			_subscriptionWriterLookup.Add("A", subscription, new LambdaTopicfilter(s => true));
			_subscriptionWriterLookup.Add("B", subscription, new LambdaTopicfilter(s => true));
			_subscriptionWriterLookup.Add("C", subscription, new LambdaTopicfilter(s => true));
			IEnumerable<Subscription> removed = _subscriptionWriterLookup.Remove("D");
			Assert.Empty(removed);
		}

		[Fact]
		public void Lookup()
		{
			Subscription subscription = new Subscription();
			_subscriptionWriterLookup.Add("A", subscription, new LambdaTopicfilter(s => true));
			(string Group, bool HashBy, string[] Writer)[] lookup = _subscriptionWriterLookup.Lookup(new Message());
			Assert.Single(lookup);
		}

		[Fact]
		public void LookupPerGroup()
		{
			_subscriptionWriterLookup.Add("A", new Subscription { Group = "A" }, new LambdaTopicfilter(s => true));
			_subscriptionWriterLookup.Add("A", new Subscription { Group = "B" }, new LambdaTopicfilter(s => true));
			(string Group, bool HashBy, string[] Writer)[] lookup = _subscriptionWriterLookup.Lookup(new Message());
			Assert.Equal(2, lookup.Length);
		}

		[Fact]
		public void LookupPerGroupPerHash()
		{
			_subscriptionWriterLookup.Add("A", new Subscription { Group = "A", HashBy = true }, new LambdaTopicfilter(s => true));
			_subscriptionWriterLookup.Add("B", new Subscription { Group = "A", HashBy = false }, new LambdaTopicfilter(s => true));
			_subscriptionWriterLookup.Add("C", new Subscription { Group = "B" }, new LambdaTopicfilter(s => true));
			(string Group, bool HashBy, string[] Writer)[] lookup = _subscriptionWriterLookup.Lookup(new Message());
			Assert.Equal(3, lookup.Length);
		}

		[Fact]
		public void LookupEmptyGroups()
		{
			_subscriptionWriterLookup.Add("A", new Subscription(), new LambdaTopicfilter(s => true));
			_subscriptionWriterLookup.Add("B", new Subscription(), new LambdaTopicfilter(s => true));
			_subscriptionWriterLookup.Add("C", new Subscription(), new LambdaTopicfilter(s => true));
			(string Group, bool HashBy, string[] Writer)[] lookup = _subscriptionWriterLookup.Lookup(new Message());
			Assert.Single(lookup);
			Assert.Equal(3, lookup[0].Writer.Length);
		}

		[Fact]
		public void CanGetAll()
		{
			_subscriptionWriterLookup.Add("A", new Subscription { Group = "A", HashBy = true }, new LambdaTopicfilter(s => true));
			_subscriptionWriterLookup.Add("B", new Subscription { Group = "A", HashBy = false }, new LambdaTopicfilter(s => true));
			_subscriptionWriterLookup.Add("C", new Subscription { Group = "A", HashBy = false }, new LambdaTopicfilter(s => true));
			_subscriptionWriterLookup.Add("D", new Subscription { Group = "B" }, new LambdaTopicfilter(s => true));
			(string, Subscription)[] all = _subscriptionWriterLookup.GetAll();
			Assert.Equal(4, all.Length);
		}
	}
}
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using Proto;

namespace ProtoBroker.Playground
{
	public interface IDistributedHashSet<TKey> : ISet<TKey>
	{

	}

	public class HashSetActor<TKey> : IActor, IDistributedHashSet<TKey>
	{
		private readonly HashSet<TKey> _hashSet;

		public HashSetActor(string name,
			IEqualityComparer<TKey> equalityComparer)
		{
			_hashSet = new HashSet<TKey>(equalityComparer);
		}

		public Task ReceiveAsync(IContext context)
		{
			switch (context.Message)
			{
			}

			return Task.CompletedTask;
		}

		public IEnumerator<TKey> GetEnumerator()
		{
			throw new System.NotImplementedException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Add(TKey item)
		{
			throw new System.NotImplementedException();
		}

		public void ExceptWith(IEnumerable<TKey> other)
		{
			throw new System.NotImplementedException();
		}

		public void IntersectWith(IEnumerable<TKey> other)
		{
			throw new System.NotImplementedException();
		}

		public bool IsProperSubsetOf(IEnumerable<TKey> other)
		{
			throw new System.NotImplementedException();
		}

		public bool IsProperSupersetOf(IEnumerable<TKey> other)
		{
			throw new System.NotImplementedException();
		}

		public bool IsSubsetOf(IEnumerable<TKey> other)
		{
			throw new System.NotImplementedException();
		}

		public bool IsSupersetOf(IEnumerable<TKey> other)
		{
			throw new System.NotImplementedException();
		}

		public bool Overlaps(IEnumerable<TKey> other)
		{
			throw new System.NotImplementedException();
		}

		public bool SetEquals(IEnumerable<TKey> other)
		{
			throw new System.NotImplementedException();
		}

		public void SymmetricExceptWith(IEnumerable<TKey> other)
		{
			throw new System.NotImplementedException();
		}

		public void UnionWith(IEnumerable<TKey> other)
		{
			throw new System.NotImplementedException();
		}

		bool ISet<TKey>.Add(TKey item)
		{
			throw new System.NotImplementedException();
		}

		public void Clear()
		{
			throw new System.NotImplementedException();
		}

		public bool Contains(TKey item)
		{
			throw new System.NotImplementedException();
		}

		public void CopyTo(TKey[] array, int arrayIndex)
		{
			throw new System.NotImplementedException();
		}

		public bool Remove(TKey item)
		{
			throw new System.NotImplementedException();
		}

		public int Count { get; }
		public bool IsReadOnly { get; }
	}
}
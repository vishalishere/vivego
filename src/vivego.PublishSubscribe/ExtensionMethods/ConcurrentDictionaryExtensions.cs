using System;
using System.Collections.Concurrent;

namespace vivego.PublishSubscribe.ExtensionMethods
{
	public static class ConcurrentDictionaryExtensions
	{
		public static TValue SecureAddOrGet<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> concurrentDictionary,
			TKey key,
			Func<TKey, TValue> addValueFactory,
			Action<TKey, TValue> dispose = null)
		{
			TValue comparisonValue;
			while (!concurrentDictionary.TryGetValue(key, out comparisonValue))
			{
				TValue newValue = addValueFactory(key);
				if (concurrentDictionary.TryAdd(key, newValue))
				{
					return newValue;
				}

				// Race condition situation, dispose newly created value(if possible)
				dispose?.Invoke(key, newValue);
			}

			return comparisonValue;
		}
	}
}
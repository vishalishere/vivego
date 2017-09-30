using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace vivego.core
{
	public static class EnumerableExtensions
	{
		/// <summary>
		///     Adds one or more sequences to the begin of the current sequence
		/// </summary>
		/// <typeparam name="T">Sequence element type</typeparam>
		/// <param name="target">Initial sequence</param>
		/// <param name="enums">Sequences to concat</param>
		/// <returns>United sequences</returns>
		public static IEnumerable<T> AddToBegin<T>(this IEnumerable<T> target, params IEnumerable<T>[] enums)
		{
			foreach (IEnumerable<T> sequence in enums.EmptyIfNull())
			{
				foreach (T item in sequence.EmptyIfNull())
				{
					yield return item;
				}
			}

			foreach (T item in target.EmptyIfNull())
			{
				yield return item;
			}
		}

		/// <summary>
		///     Adds one or more elements to the begin of sequence
		/// </summary>
		/// <typeparam name="T">Sequence element type</typeparam>
		/// <param name="target">Initial sequence</param>
		/// <param name="values">Elements to concat</param>
		/// <returns>United sequences</returns>
		public static IEnumerable<T> AddToBegin<T>(this IEnumerable<T> target, params T[] values)
		{
			foreach (T value in values.EmptyIfNull())
			{
				yield return value;
			}

			foreach (T item in target.EmptyIfNull())
			{
				yield return item;
			}
		}

		/// <summary>
		///     Adds one or more sequences to the end of the current sequence
		/// </summary>
		/// <typeparam name="T">Sequence element type</typeparam>
		/// <param name="target">Initial sequence</param>
		/// <param name="enums">Sequences to concat</param>
		/// <returns>United sequences</returns>
		public static IEnumerable<T> AddToEnd<T>(this IEnumerable<T> target, params IEnumerable<T>[] enums)
		{
			foreach (T item in target.EmptyIfNull())
			{
				yield return item;
			}

			foreach (IEnumerable<T> sequence in enums.EmptyIfNull())
			{
				foreach (T item in sequence.EmptyIfNull())
				{
					yield return item;
				}
			}
		}

		/// <summary>
		///     Adds one or more elements to sequence
		/// </summary>
		/// <typeparam name="T">Sequence element type</typeparam>
		/// <param name="target">Initial sequence</param>
		/// <param name="values">Elements to concat</param>
		/// <returns>United sequences</returns>
		public static IEnumerable<T> AddToEnd<T>(this IEnumerable<T> target, params T[] values)
		{
			foreach (T item in target.EmptyIfNull())
			{
				yield return item;
			}

			foreach (T value in values.EmptyIfNull())
			{
				yield return value;
			}
		}

		public static T Coalesce<T>(this IEnumerable<T> enumerable)
		{
			return enumerable
				.EmptyIfNull()
				.FirstOrDefault(e => !e.IsNull());
		}

		public static IEnumerable<T> Delete<T>(this IEnumerable<T> target, int position, int length)
		{
			int pos = 0;
			foreach (T item in target.WhereNotNull())
			{
				if (pos == position && length > 0)
				{
					length--;
					continue;
				}

				pos++;
				yield return item;
			}
		}

		public static IEnumerable<T> Delete<T>(this IEnumerable<T> target, T element)
		{
			return target
				.EmptyIfNull()
				.Where(item => item.Equals(element));
		}

		/// <summary>
		///     If sequence is null then an empty list is returned, otherwise the sequence.
		/// </summary>
		/// <param name="sequence"></param>
		/// <returns></returns>
		public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> sequence)
		{
			return sequence ?? Enumerable.Empty<T>();
		}

		/// <summary>
		///     Determines whether the enumerable contains elements that match the conditions defined by the specified predicate
		/// </summary>
		/// <typeparam name="T">Sequence element type</typeparam>
		/// <param name="enumerable">Target enumeration</param>
		/// <param name="predicate">Condition of the element to search for</param>
		/// <returns>true, if specified element is existed, otherwise, false</returns>
		/// <exception cref="System.ArgumentNullException">One of the input agruments is null</exception>
		public static bool Exists<T>(this IEnumerable<T> enumerable, Predicate<T> predicate)
		{
			return enumerable
				.EmptyIfNull()
				.Any(item => predicate(item));
		}

		/// <summary>
		///     Iterates through all sequence and performs specified action on each
		///     element
		/// </summary>
		/// <typeparam name="T">Sequence element type</typeparam>
		/// <param name="enumerable">Target enumeration</param>
		/// <param name="action">Action</param>
		/// <exception cref="System.ArgumentNullException">One of the input agruments is null</exception>
		public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
		{
			foreach (T elem in enumerable.EmptyIfNull())
			{
				action(elem);
			}
		}

		public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
		{
			if (enumerable == null)
			{
				return true;
			}

			if (enumerable is IList list)
			{
				if (list.Count == 0)
				{
					return true;
				}
			}
			else if (!enumerable.Any())
			{
				return true;
			}

			return false;
		}

		public static string Join<T>(this IEnumerable<T> target, string separator)
		{
			return target == null
				? string.Empty
				: string.Join(separator, target.EmptyIfNull().Select(i => i.IsNull() ? string.Empty : i.ToString()).ToArray());
		}

		public static IEnumerable<IEnumerable<T>> Partition<T>(this IEnumerable<T> source, int pageSize)
		{
			using (IEnumerator<T> enumerator = source.EmptyIfNull().GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					yield return enumerator.Take(pageSize);
				}
			}
		}

		public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> enumerable)
		{
			return enumerable
				.EmptyIfNull()
				.Where(t => !t.IsNull());
		}

		public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> enumerable, Predicate<T> predicate)
		{
			return enumerable
				.EmptyIfNull()
				.Where(t => !t.IsNull() && predicate(t));
		}

		private static IEnumerable<T> Take<T>(this IEnumerator<T> source, int pageSize)
		{
			do
			{
				pageSize--;
				yield return source.Current;
			} while (pageSize > 0 && source.MoveNext());
		}

		/// <summary>
		///     Returns all distinct elements of the given source, where "distinctness"
		///     is determined via a projection and the default equality comparer for the projected type.
		/// </summary>
		/// <remarks>
		///     This operator uses deferred execution and streams the results, although
		///     a set of already-seen keys is retained. If a key is seen multiple times,
		///     only the first element with that key is returned.
		/// </remarks>
		/// <typeparam name="TSource">Type of the source sequence</typeparam>
		/// <typeparam name="TKey">Type of the projected element</typeparam>
		/// <param name="source">Source sequence</param>
		/// <param name="keySelector">Projection for determining "distinctness"</param>
		/// <returns>
		///     A sequence consisting of distinct elements from the source sequence,
		///     comparing them by the specified key projection.
		/// </returns>
		public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector)
		{
			return source
				.EmptyIfNull()
				.DistinctBy(keySelector, null);
		}

		/// <summary>
		///     Returns all distinct elements of the given source, where "distinctness"
		///     is determined via a projection and the specified comparer for the projected type.
		/// </summary>
		/// <remarks>
		///     This operator uses deferred execution and streams the results, although
		///     a set of already-seen keys is retained. If a key is seen multiple times,
		///     only the first element with that key is returned.
		/// </remarks>
		/// <typeparam name="TSource">Type of the source sequence</typeparam>
		/// <typeparam name="TKey">Type of the projected element</typeparam>
		/// <param name="source">Source sequence</param>
		/// <param name="keySelector">Projection for determining "distinctness"</param>
		/// <param name="comparer">
		///     The equality comparer to use to determine whether or not keys are equal.
		///     If null, the default equality comparer for <c>TSource</c> is used.
		/// </param>
		/// <returns>
		///     A sequence consisting of distinct elements from the source sequence,
		///     comparing them by the specified key projection.
		/// </returns>
		public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
		{
			if (keySelector == null)
			{
				throw new ArgumentNullException(nameof(keySelector));
			}

			return DistinctByImpl(source, keySelector, comparer);
		}

		private static IEnumerable<TSource> DistinctByImpl<TSource, TKey>(IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
		{
			HashSet<TKey> knownKeys = new HashSet<TKey>(comparer);
			return source
				.EmptyIfNull()
				.Where(element => knownKeys.Add(keySelector(element)));
		}

		public static IEnumerable<TSource> ExceptBy<TSource>(this IEnumerable<TSource> source,
			Predicate<TSource> keySelector,
			IEqualityComparer<TSource> equalityComparer = null)
		{
			IEnumerable<TSource> sourceArray = source as TSource[] ?? source.ToArray();
			return sourceArray
				.EmptyIfNull()
				.Except(sourceArray
					.EmptyIfNull()
					.Where(s => keySelector(s)), equalityComparer);
		}
	}
}

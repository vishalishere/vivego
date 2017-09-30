using System.Collections.Generic;
using System.Linq;

namespace vivego.core
{
	public static class ObjectExtensions
	{
		/// <summary>
		///     Checks is a value is contained in a list of the same type
		/// </summary>
		/// <typeparam name="T"> </typeparam>
		/// <param name="object"> </param>
		/// <param name="parameters"> </param>
		/// <returns> </returns>
		public static bool IsIn<T>(this T @object, params T[] parameters)
		{
			return parameters.Contains(@object);
		}

		public static bool IsIn<T>(this T @object, IEqualityComparer<T> comparer, params T[] parameters)
		{
			return parameters.Contains(@object, comparer);
		}

		/// <summary>
		///     Check if an object is null, same as @object == null
		/// </summary>
		/// <typeparam name="T"> </typeparam>
		/// <param name="object"> </param>
		/// <returns> </returns>
		public static bool IsNull<T>(this T @object)
		{
			return Equals(@object, default);
		}
	}
}
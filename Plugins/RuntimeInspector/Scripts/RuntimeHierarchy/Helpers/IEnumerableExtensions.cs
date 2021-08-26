
using System.Collections.Generic;
using System.Linq;

namespace RuntimeInspectorNamespace
{
	public static class IEnumerableExtensions
	{
		public static bool IsNullOrEmpty<T>( this IEnumerable<T> enumerable )
			=> enumerable == null || enumerable.Count() == 0;
	}
}

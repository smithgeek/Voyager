using System.Collections.Generic;
using System.Linq;

namespace Voyager.ModelBinding;

internal static class Extensions
{
	public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
	{
		return (IEnumerable<T>)source.Where(x => x != null);
	}

}

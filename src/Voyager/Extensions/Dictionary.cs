using System.Collections.Generic;

namespace Voyager.Extensions;

public static class Dictionary
{
	public static void ReplaceKey<TKey, TValue>(this IDictionary<TKey, TValue> validationErrors, TKey oldKey, TKey newKey)
	{
		if (validationErrors.TryGetValue(oldKey, out var value))
		{
			validationErrors.Remove(oldKey);
			validationErrors.TryAdd(newKey, value);
		}
	}
}
